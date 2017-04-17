using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Document = Microsoft.CodeAnalysis.Document;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;
using static AsyncGenerator.Analyzation.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Analyzation
{
	public partial class ProjectAnalyzer
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProjectAnalyzer));

		private IImmutableSet<Document> _analyzeDocuments;
		private IImmutableSet<Project> _analyzeProjects;
		private ProjectAnalyzeConfiguration _configuration;
		private Solution _solution;
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
			var documentData = await Task.WhenAll(_analyzeDocuments.Select(o => ProjectData.CreateDocumentData(o))).ConfigureAwait(false);
			Logger.Info("Parsing documents completed");

			// 2. Step - Each method in a document will be pre-analyzed and saved in a structural tree
			Logger.Info("Pre-analyzing documents started");
			Parallel.ForEach(documentData, PreAnalyzeDocumentData);
			//await Task.WhenAll(documentData.AsParallel()..Select(PreAnalyzeDocumentData)).ConfigureAwait(false);
			Logger.Info("Pre-analyzing documents completed");

			// 3. Step - Find all references for each method and optionally scan its body for async counterparts
			Logger.Info("Scanning references started");
			await Task.WhenAll(documentData.Select(ScanDocumentData)).ConfigureAwait(false);
			Logger.Info("Scanning references completed");

			// 4. Step - Analyze all references found in the previous step
			Logger.Info("Analyzing documents started");
			Parallel.ForEach(documentData, AnalyzeDocumentData);
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

		private IEnumerable<IMethodSymbol> GetAsyncCounterparts(IMethodSymbol methodSymbol, AsyncCounterpartsSearchOptions options, bool onlyNew = false)
		{
			var dict = _methodAsyncConterparts.GetOrAdd(methodSymbol, new ConcurrentDictionary<AsyncCounterpartsSearchOptions, HashSet<IMethodSymbol>>());
			HashSet<IMethodSymbol> asyncMethodSymbols;
			if (dict.TryGetValue(options, out asyncMethodSymbols))
			{
				return onlyNew ? Enumerable.Empty<IMethodSymbol>() : asyncMethodSymbols;
			}
			asyncMethodSymbols = new HashSet<IMethodSymbol>(_configuration.FindAsyncCounterpartsFinders
				.SelectMany(o => o.FindAsyncCounterparts(methodSymbol, options)));
			return dict.AddOrUpdate(
				options,
				asyncMethodSymbols,
				(k, v) =>
				{
					Logger.Debug($"Performance hit: Multiple GetAsyncCounterparts method calls for method symbol {methodSymbol}");
					return asyncMethodSymbols;
				});
		}

		/// <summary>
		/// Find all invoked methods that have an async counterpart and have not been discovered yet.
		/// </summary>
		/// <param name="methodData">The method data to be searched</param>
		/// <returns>Collection of invoked methods that have an async counterpart</returns>
		private IEnumerable<IMethodSymbol> FindNewlyInvokedMethodsWithAsyncCounterpart(MethodData methodData)
		{
			var result = new HashSet<IMethodSymbol>();
			var methodDataBody = methodData.GetBodyNode();
			if (methodDataBody == null)
			{
				return result;
			}
			var documentData = methodData.TypeData.NamespaceData.DocumentData;
			var semanticModel = documentData.SemanticModel;
			var searchOptions = Default;
			if (_configuration.UseCancellationTokenOverload)
			{
				searchOptions |= HasCancellationToken;
			}

			foreach (var invocation in methodDataBody.DescendantNodes()
										   .OfType<InvocationExpressionSyntax>())
			{
				var methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
				if (methodSymbol == null)
				{
					continue;
				}
				methodSymbol = methodSymbol.OriginalDefinition;
				if (result.Contains(methodSymbol))
				{
					continue;
				}
				var asyncCounterparts = GetAsyncCounterparts(methodSymbol, searchOptions, true).ToList();
				if (asyncCounterparts.Any())
				{
					result.Add(methodSymbol);
				}
			}
			return result;
		}

		private void Setup()
		{
			_configuration = ProjectData.Configuration.AnalyzeConfiguration;
			_solution = ProjectData.Project.Solution;
			_analyzeDocuments = ProjectData.Project.Documents.Where(o => _configuration.DocumentSelectionPredicate(o))
				.ToImmutableHashSet();
			_analyzeProjects = new[] { ProjectData.Project }.ToImmutableHashSet();
		}
	}
}
