using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using log4net;
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
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProjectAnalyzer));

		private IImmutableSet<Document> _analyzeDocuments;
		private IImmutableSet<Project> _analyzeProjects;
		private ProjectAnalyzeConfiguration _configuration;
		private Solution _solution;
		private readonly ConcurrentDictionary<ITypeSymbol, ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>> _methodByTypeAsyncConterparts = 
			new ConcurrentDictionary<ITypeSymbol, ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>>();
		private readonly ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>> _methodAsyncConterparts =
			new ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>>();

		public ProjectAnalyzer(ProjectData projectData)
		{
			ProjectData = projectData;
		}

		public ProjectData ProjectData { get; }

		public async Task<IProjectAnalyzationResult> Analyze()
		{
			Setup();

			// 1. Step - Parse all documents inside the project and create a DocumentData for each
			Logger.Info("Parsing documents started");
			DocumentData[] documentData;
			if (_configuration.ConcurrentRun)
			{
				documentData = await Task.WhenAll(_analyzeDocuments.Select(o => ProjectData.CreateDocumentData(o)))
					.ConfigureAwait(false);
			}
			else
			{
				documentData = new DocumentData[_analyzeDocuments.Count];
				var i = 0;
				foreach (var analyzeDocument in _analyzeDocuments)
				{
					documentData[i] = await ProjectData.CreateDocumentData(analyzeDocument).ConfigureAwait(false);
					i++;
				}
			}
			Logger.Info("Parsing documents completed");

			// 2. Step - Each method in a document will be pre-analyzed and saved in a structural tree
			Logger.Info("Pre-analyzing documents started");
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
			Logger.Info("Pre-analyzing documents completed");

			// 3. Step - Find all references for each method and optionally scan its body for async counterparts
			Logger.Info("Scanning references started");
			if (_configuration.ConcurrentRun)
			{
				await Task.WhenAll(documentData.Select(ScanDocumentData)).ConfigureAwait(false);
			}
			else
			{
				foreach (var item in documentData)
				{
					await ScanDocumentData(item).ConfigureAwait(false);
				}
			}
			Logger.Info("Scanning references completed");

			Logger.Info(
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
			Logger.Info("Analyzing documents started");
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
			Logger.Info("Analyzing documents completed");

			// 5. Step - Calculate the final conversion for all method data
			Logger.Info("Post-analyzing documents started");
			PostAnalyze(documentData);
			Logger.Info("Post-analyzing documents completed");

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
			HashSet<IMethodSymbol> asyncMethodSymbols;
			if (dict.TryGetValue(options, out asyncMethodSymbols))
			{
				return onlyNew ? Enumerable.Empty<IMethodSymbol>() : asyncMethodSymbols;
			}
			asyncMethodSymbols = new HashSet<IMethodSymbol>(_configuration.FindAsyncCounterpartsFinders
				.SelectMany(o => o.FindAsyncCounterparts(methodSymbol, invokedFromType, options)));
			return dict.AddOrUpdate(
				options,
				asyncMethodSymbols,
				(k, v) =>
				{
					Logger.Debug($"Performance hit: Multiple GetAsyncCounterparts method calls for method symbol {methodSymbol}");
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
			if (!_scannedMethodBodies.TryAdd(methodData))
			{
				return Enumerable.Empty<IMethodSymbol>();
			}

			var result = new HashSet<IMethodSymbol>();
			var methodDataBody = methodData.GetBodyNode();
			if (methodDataBody == null)
			{
				return result;
			}
			var documentData = methodData.TypeData.NamespaceData.DocumentData;
			var semanticModel = documentData.SemanticModel;
			var searchOptions = AsyncCounterpartsSearchOptions.Default;
			if (_configuration.UseCancellationTokens || _configuration.ScanForMissingAsyncMembers != null)
			{
				searchOptions |= AsyncCounterpartsSearchOptions.HasCancellationToken;
			}

			foreach (var node in methodDataBody.DescendantNodes().Where(o => o.IsKind(SyntaxKind.InvocationExpression) || o.IsKind(SyntaxKind.IdentifierName)))
			{
				IMethodSymbol methodSymbol = null;
				ITypeSymbol typeSymbol = null;
				var invocation = node as InvocationExpressionSyntax;
				if (invocation != null)
				{
					methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
					if (invocation.Expression is SimpleNameSyntax)
					{
						typeSymbol = methodData.Symbol.ContainingType;
					}
					else if (invocation.Expression is MemberAccessExpressionSyntax memberAccessExpression)
					{
						typeSymbol = semanticModel.GetTypeInfo(memberAccessExpression.Expression).Type;
					}
				}
				else if (node is IdentifierNameSyntax identifier)
				{
					if (identifier.Identifier.ToString() == "var")
					{
						continue;
					}
					var propertySymbol = semanticModel.GetSymbolInfo(identifier).Symbol as IPropertySymbol;
					if (propertySymbol == null)
					{
						continue;
					}
					typeSymbol = propertySymbol.ContainingType;
					methodSymbol = identifier.IsAssigned() ? propertySymbol.SetMethod : propertySymbol.GetMethod;
				}
				if (methodSymbol == null)
				{
					continue;
				}

				methodSymbol = methodSymbol.OriginalDefinition;
				if (result.Contains(methodSymbol))
				{
					continue;
				}
				// If an internal method was ignored from searching its references but we found out that it is used inside the project,
				// we must override the user setting and search for its references in order to prevent generating an invalid code
				if (!_configuration.SearchForMethodReferences(methodSymbol) && ProjectData.Contains(methodSymbol) &&
					_mustScanForMethodReferences.TryAdd(methodSymbol))
				{
					searchReferences.Add(methodSymbol);
					Logger.Warn($"Overriding SearchForMethodReferences user setting for method {methodSymbol} as we found a reference to it");
				}

				if (!_configuration.SearchForAsyncCounterparts(methodSymbol))
				{
					continue;
				}
				// Add method only if new
				if (GetAsyncCounterparts(methodSymbol, typeSymbol, searchOptions, true).Any())
				{
					result.Add(methodSymbol);
				}
				if (invocation == null || !GetAsyncCounterparts(methodSymbol, typeSymbol, searchOptions).Any())
				{
					continue;
				}

				// Check if there is any method passed as argument that have also an async counterpart
				foreach (var argument in invocation.ArgumentList.Arguments
					.Where(o => o.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) || o.Expression.IsKind(SyntaxKind.IdentifierName)))
				{
					var argMethodSymbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol as IMethodSymbol;
					if (argMethodSymbol == null)
					{
						continue;
					}
					if (GetAsyncCounterparts(argMethodSymbol.OriginalDefinition, searchOptions, true).Any())
					{
						result.Add(argMethodSymbol);
					}
				}
			}
			return result;
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
		}

		private void LogIgnoredReason(AbstractData functionData, bool warn = false)
		{
			var message = $"Method {functionData.GetSymbol()} was ignored. Reason: {functionData.IgnoredReason}";
			if (warn)
			{
				Logger.Warn(message);
			}
			else
			{
				Logger.Debug(message);
			}
		}

		private void WarnLogIgnoredReason(AbstractData functionData)
		{
			LogIgnoredReason(functionData, true);
		}

		private void VoidLog(AbstractData functionData)
		{
		}
	}
}
