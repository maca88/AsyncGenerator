using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Logging;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Document = Microsoft.CodeAnalysis.Document;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace AsyncGenerator.Analyzation.Internal
{
	internal partial class ProjectAnalyzer
	{
		private readonly ILogger _logger;
		private readonly ILogger _diagnosticsLogger;
		private AsyncCounterpartsSearchOptions _searchOptions;
		private IImmutableSet<Document> _analyzeDocuments;
		private IImmutableSet<Project> _analyzeProjects;
		private ProjectAnalyzeConfiguration _configuration;
		private Solution _solution;
		private readonly ConcurrentDictionary<ITypeSymbol, ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>> _methodByTypeAsyncConterparts = 
			new ConcurrentDictionary<ITypeSymbol, ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>>();
		private readonly ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>> _methodAsyncConterparts =
			new ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>();

		public ProjectAnalyzer(ProjectData projectData, ILoggerFactory loggerFactory)
		{
			ProjectData = projectData;
			_logger = loggerFactory.GetLogger($"{nameof(AsyncGenerator)}.{nameof(ProjectAnalyzer)}");
			_diagnosticsLogger = loggerFactory.GetLogger($"{nameof(AsyncGenerator)}.{nameof(ProjectAnalyzer)}.Diagnostics");
		}

		public ProjectData ProjectData { get; }

		public async Task<IProjectAnalyzationResult> Analyze(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			Setup();

			// 1. Step - Parse all documents inside the project and create a DocumentData for each
			_logger.Info("Parsing documents started");
			DocumentData[] documentData;
			if (_configuration.ConcurrentRun)
			{
				documentData = await Task.WhenAll(_analyzeDocuments.Select(o => ProjectData.CreateDocumentData(o, cancellationToken)))
					.ConfigureAwait(false);
			}
			else
			{
				documentData = new DocumentData[_analyzeDocuments.Count];
				var i = 0;
				foreach (var analyzeDocument in _analyzeDocuments)
				{
					documentData[i] = await ProjectData.CreateDocumentData(analyzeDocument, cancellationToken).ConfigureAwait(false);
					i++;
				}
			}
			_logger.Info("Parsing documents completed");

			// 2. Step - Each method in a document will be pre-analyzed and saved in a structural tree
			_logger.Info("Pre-analyzing documents started");
			if (_configuration.ConcurrentRun)
			{
				Parallel.ForEach(documentData, PreAnalyzeDocumentData);
			}
			else
			{
				foreach (var item in documentData)
				{
					PreAnalyzeDocumentData(item);
				}
			}
			_logger.Info("Pre-analyzing documents completed");

			// 3. Step - Find all references for each method and optionally scan its body for async counterparts
			_logger.Info("Scanning references started");
			if (_configuration.ConcurrentRun)
			{
				await Task.WhenAll(documentData.Select(o => ScanDocumentData(o, cancellationToken))).ConfigureAwait(false);
			}
			else
			{
				foreach (var item in documentData)
				{
					await ScanDocumentData(item, cancellationToken).ConfigureAwait(false);
				}
			}
			_logger.Info("Scanning references completed");

			_logger.Debug(
				$"Scanning statistics:{Environment.NewLine}" +
				$"Total scanned documents: {documentData.Length}{Environment.NewLine}" +
				$"Total scanned methods: {_scannedMethodOrAccessors.Count}{Environment.NewLine}" +
				$"Total method implementations searched: {_searchedImplementations.Count}{Environment.NewLine}" +
				$"Total method overrides searched: {_searchedOverrides.Count}{Environment.NewLine}" +
				$"Total method references searched: {_searchedMethodReferences.Count}{Environment.NewLine}" +
				$"Total method reference locations scanned: {_scannedLocationsSymbols.Count}{Environment.NewLine}" +
				$"Total method bodies scanned: {_scannedMethodBodies.Count}{Environment.NewLine}" +
				$"Max scanned depth: {_maxScanningDepth}"
			);

			// 4. Step - Analyze all references found in the previous step
			_logger.Info("Analyzing documents started");
			if (_configuration.ConcurrentRun)
			{
				Parallel.ForEach(documentData, AnalyzeDocumentData);
			}
			else
			{
				foreach (var item in documentData)
				{
					AnalyzeDocumentData(item);
				}
			}
			_logger.Info("Analyzing documents completed");

			// 5. Step - Calculate the final conversion for all method data
			_logger.Info("Post-analyzing documents started");
			PostAnalyze(documentData);
			_logger.Info("Post-analyzing documents completed");

			// 6. Step - Log the diagnoses of documents
			if (_configuration.Diagnostics.Enabled)
			{
				_logger.Info("Diagnose of documents started");
				Diagnose(documentData);
				_logger.Info("Diagnose of documents completed");
			}
			return ProjectData;
		}

		private bool CanProcessDocument(Document doc)
		{
			if (doc.Project != ProjectData.Project)
			{
				return false;
			}
			return _analyzeDocuments.Contains(doc);
		}

		private IEnumerable<IMethodSymbol> GetAsyncCounterparts(IMethodSymbol methodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options, bool onlyNew = false)
		{
			if (invokedFromType == null)
			{
				return GetAsyncCounterparts(methodSymbol, options, onlyNew);
			}
			var typeDict = _methodByTypeAsyncConterparts.GetOrAdd(invokedFromType.OriginalDefinition, new ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>());
			return GetAsyncCounterparts(typeDict, methodSymbol, invokedFromType.OriginalDefinition, options, onlyNew);
		}

		private IEnumerable<IMethodSymbol> GetAsyncCounterparts(IMethodSymbol methodSymbol, AsyncCounterpartsSearchOptions options, bool onlyNew = false)
		{
			return GetAsyncCounterparts(_methodAsyncConterparts, methodSymbol, null, options, onlyNew);
		}

		private IEnumerable<IMethodSymbol> GetAsyncCounterparts(ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>> asyncCounterparts, 
			IMethodSymbol methodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options, bool onlyNew = false)
		{
			var dict = asyncCounterparts.GetOrAdd(methodSymbol, new ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>());
			if (dict.TryGetValue(options, out var asyncMethodSymbols))
			{
				return onlyNew ? Enumerable.Empty<IMethodSymbol>() : asyncMethodSymbols;
			}
			asyncMethodSymbols = _configuration.SearchForAsyncCounterparts(methodSymbol)
				? new HashSet<IMethodSymbol>(_configuration.FindAsyncCounterpartsFinders
					.SelectMany(o => o.FindAsyncCounterparts(methodSymbol, invokedFromType, options)))
				: new HashSet<IMethodSymbol>();
			return dict.AddOrUpdate(
				options,
				asyncMethodSymbols,
				(k, v) =>
				{
					_logger.Debug($"Performance hit: Multiple GetAsyncCounterparts method calls for method symbol {methodSymbol}");
					return asyncMethodSymbols;
				});
		}


		private readonly ConcurrentSet<FunctionData> _scannedMethodBodies = new ConcurrentSet<FunctionData>();

		private readonly ConcurrentSet<IMethodSymbol> _mustScanForMethodReferences = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Find all invoked methods that have an async counterpart and have not been discovered yet.
		/// </summary>
		/// <param name="methodData">The method data to be searched</param>
		/// <param name="searchReferences">A set where the methods that had an invalid SearchForMethodReferences setting will be added</param>
		/// <returns>Collection of invoked methods that have an async counterpart</returns>
		private IEnumerable<IMethodSymbol> FindNewlyInvokedMethodsWithAsyncCounterpart(FunctionData methodData, ISet<IMethodSymbol> searchReferences)
		{
			if (!_scannedMethodBodies.TryAdd(methodData) || methodData.GetBodyNode() == null)
			{
				return Enumerable.Empty<IMethodSymbol>();
			}

			var result = new HashSet<IMethodSymbol>();
			var documentData = methodData.TypeData.NamespaceData.DocumentData;
			var semanticModel = documentData.SemanticModel;

			foreach (var functionData in methodData.GetSelfAndDescendantsFunctions())
			{
				ProcessFunctionData(functionData);
			}

			return result;

			void ProcessFunctionData(FunctionData functionData)
			{
				var bodyNode = functionData.GetBodyNode();
				if (bodyNode == null)
				{
					return;
				}

				foreach (var node in bodyNode.DescendantNodes(o => !o.IsFunction())
					.Where(o => o.IsKind(SyntaxKind.InvocationExpression) || o.IsKind(SyntaxKind.IdentifierName)))
				{
					IMethodSymbol methodSymbol = null;
					ITypeSymbol typeSymbol = null;
					var invocation = node as InvocationExpressionSyntax;
					if (invocation != null)
					{
						switch (invocation.Expression)
						{
							case SimpleNameSyntax _:
								typeSymbol = functionData.Symbol.ContainingType;
								break;
							case MemberAccessExpressionSyntax memberAccessExpression:
								typeSymbol = semanticModel.GetTypeInfo(memberAccessExpression.Expression).Type;
								break;
						}

						if (invocation.Expression.ToString() == "nameof")
						{
							methodSymbol = semanticModel.GetSymbolInfo(
								invocation.ArgumentList.Arguments.First().Expression)
								.CandidateSymbols
								.OfType<IMethodSymbol>()
								.FirstOrDefault();
						}
						else
						{
							var symbolInfo = semanticModel.GetSymbolInfo(invocation.Expression);
							// Will happen for dynamic
							if (symbolInfo.Symbol == null)
							{
								var candidates = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToList();
								methodSymbol = candidates.FirstOrDefault();
								foreach (var candidateSymbol in candidates.Skip(1))
								{
									ProcessMethod(candidateSymbol, typeSymbol, invocation);
								}
							}
							else
							{
								switch (symbolInfo.Symbol)
								{
									case IMethodSymbol mSymbol:
										methodSymbol = mSymbol;
										break;
									//case ILocalSymbol localSymbol:
									//	break;
								}
							}
						}
					}
					else if (node is IdentifierNameSyntax identifier)
					{
						if (identifier.Identifier.ToString() == "var")
						{
							continue;
						}

						var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
						if (symbol is IMethodSymbol mSymbol)
						{
							methodSymbol = mSymbol;
							typeSymbol = methodSymbol.ContainingType;
						}
						else if (symbol is IPropertySymbol propertySymbol)
						{
							typeSymbol = propertySymbol.ContainingType;
							var isAssigned = identifier.IsAssigned();
							methodSymbol = isAssigned ? propertySymbol.SetMethod : propertySymbol.GetMethod;
							// Auto-properties are skipped as they can never throw a non fatal exception
							if (!functionData.WrapInTryCatch &&
								(methodSymbol?.IsVirtualAbstractOrInterface() == true || methodSymbol?.IsAutoPropertyAccessor() != true) &&
								node.Ancestors().First(o => o.IsFunction()) == functionData.GetNode())
							{
								if (isAssigned)
								{
									functionData.WrapInTryCatch = true;
								}
								// Here we don't know if there is any precondition
								else if (_configuration.ExceptionHandling.CatchPropertyGetterCalls(methodSymbol))
								{
									// TODO: move elsewhere
									functionData.CatchPropertyGetterCalls.Add(identifier);
								}
							}
						}
						else
						{
							continue;
						}

					}

					if (!ProcessMethod(methodSymbol, typeSymbol, invocation))
					{
						continue;
					}

					// Check if there is any method passed as argument that have also an async counterpart
					// ReSharper disable once PossibleNullReferenceException
					foreach (var argument in invocation.ArgumentList.Arguments
						.Where(o => o.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) || o.Expression.IsKind(SyntaxKind.IdentifierName)))
					{
						if (!(semanticModel.GetSymbolInfo(argument.Expression).Symbol is IMethodSymbol argMethodSymbol))
						{
							continue;
						}
						if (GetAsyncCounterparts(argMethodSymbol.OriginalDefinition, _searchOptions, true).Any())
						{
							result.Add(argMethodSymbol);
						}
					}
				}
			}

			bool ProcessMethod(IMethodSymbol methodSymbol, ITypeSymbol typeSymbol, InvocationExpressionSyntax invocation)
			{
				if (methodSymbol == null)
				{
					return false;
				}

				methodSymbol = methodSymbol.OriginalDefinition;
				if (result.Contains(methodSymbol))
				{
					return false;
				}
				// If an internal method was ignored from searching its references but we found out that it is used inside the project,
				// we must override the user setting and search for its references in order to prevent generating an invalid code
				if (!_configuration.SearchForMethodReferences(methodSymbol) && ProjectData.Contains(methodSymbol) &&
				    _mustScanForMethodReferences.TryAdd(methodSymbol))
				{
					searchReferences.Add(methodSymbol);
					ProjectData.GetFunctionData(methodSymbol).AddDiagnostic(
						$"Overriding SearchForMethodReferences user setting, as we found a reference inside method {methodData.Symbol}",
						DiagnosticSeverity.Info);
				}

				// Add method only if new
				if (GetAsyncCounterparts(methodSymbol, typeSymbol, _searchOptions, true).Any())
				{
					result.Add(methodSymbol);
				}
				if (invocation == null || !GetAsyncCounterparts(methodSymbol, typeSymbol, _searchOptions).Any())
				{
					return false;
				}

				return true;
			}
		}

		private void Setup()
		{
			_configuration = ProjectData.Configuration.AnalyzeConfiguration;
			_solution = ProjectData.Project.Solution;
			_analyzeDocuments = ProjectData.Project.Documents
				.Where(o => _configuration.DocumentSelectionPredicate(o))
				.ToImmutableHashSet();
			_analyzeProjects = new[] { ProjectData.Project }
				.ToImmutableHashSet();
			_searchOptions = AsyncCounterpartsSearchOptions.Default;
			var useTokens = _configuration.UseCancellationTokens | _configuration.ScanForMissingAsyncMembers != null;
			if (useTokens)
			{
				_searchOptions |= AsyncCounterpartsSearchOptions.HasCancellationToken;
			}
			if (_configuration.SearchAsyncCounterpartsInInheritedTypes)
			{
				_searchOptions |= AsyncCounterpartsSearchOptions.SearchInheritedTypes;
			}
		}
	}
}
