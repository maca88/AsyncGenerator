using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace AsyncGenerator
{
	public class ProjectData
	{
		private readonly SolutionData _solutionData;

		public ProjectData(SolutionData solutionData, ProjectId projectId, ProjectConfiguration configuration)
		{
			_solutionData = solutionData;
			Configuration = configuration;
			ProjectId = projectId;
			DirectoryPath = Path.GetDirectoryName(Project.FilePath) + @"\";
		}

		internal readonly ProjectConfiguration Configuration;

		public string DirectoryPath { get; }

		public ProjectId ProjectId { get; }

		public Project Project
		{
			get { return _solutionData.Solution.GetProject(ProjectId); }
			set { _solutionData.Solution = value.Solution; }
		}

		public ConcurrentDictionary<string, DocumentData> DocumentData { get; } = new ConcurrentDictionary<string, DocumentData>();

		public DocumentData GetDocumentData(SyntaxReference syntax)
		{
			return GetDocumentData(Project.Solution.GetDocument(syntax.SyntaxTree));
		}

		public DocumentData GetDocumentData(Document document)
		{
			DocumentData documentData;
			if (!DocumentData.TryGetValue(document.FilePath, out documentData))
			{
				throw new InvalidOperationException($"Document {document.FilePath} was not found in the project {Project.Name}");
			}
			return documentData;
		}

		public async Task<DocumentData> CreateDocumentData(Document document)
		{
			if (document.Project != Project)
			{
				throw new InvalidOperationException($"Document {document.FilePath} does not belong to project {Project.Name}");
			}
			var rootNode = (CompilationUnitSyntax)await document.GetSyntaxRootAsync().ConfigureAwait(false);
			var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
			var documentData = new DocumentData(this, document, rootNode, semanticModel);
			return DocumentData.AddOrUpdate(document.FilePath, documentData, (s, data) => documentData);
		}

	}

	public class SolutionData
	{
		public SolutionData(Solution solution, MSBuildWorkspace buildWorkspace, SolutionConfiguration configuration)
		{
			Configuration = configuration;
			Workspace = buildWorkspace;
			Solution = solution;
		}

		public MSBuildWorkspace Workspace { get; }

		public readonly SolutionConfiguration Configuration;

		public Solution Solution { get; set; }

		internal ConcurrentDictionary<ProjectId, ProjectData> ProjectData { get; } = new ConcurrentDictionary<ProjectId, ProjectData>();

	}

	public class ProjectAnalyzer
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProjectAnalyzer));

		private IImmutableSet<Document> _analyzeDocuments;
		private IImmutableSet<Project> _analyzeProjects;
		private ProjectAnalyzeConfiguration _configuration;
		private Solution _solution;
		private readonly ConcurrentDictionary<IMethodSymbol, IMethodSymbol> _methodAsyncConterparts = new ConcurrentDictionary<IMethodSymbol, IMethodSymbol>();

		public ProjectAnalyzer(ProjectData projectData)
		{
			ProjectData = projectData;
		}

		public ProjectData ProjectData { get; }

		public async Task Analyze()
		{
			Setup();
			// 1. Step - Parse all documents inside the project and create a DocumentData for each
			var documentData = await Task.WhenAll(_analyzeDocuments.Select(o => ProjectData.CreateDocumentData(o))).ConfigureAwait(false);

			// 2. Step - Each method in a document will be analyzed and saved in a structural tree
			await Task.WhenAll(documentData.Select(AnalyzeDocumentData)).ConfigureAwait(false);

			// 3. Step - Find all references for each method and optionally scan its body for async counterparts
			await Task.WhenAll(documentData.Select(ScanDocumentData)).ConfigureAwait(false);
		}

		private async Task AnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeNode in documentData.RootNode
				.DescendantNodes()
				.OfType<TypeDeclarationSyntax>())
			{
				var typeData = documentData.GetOrCreateTypeData(typeNode);
				typeData.Conversion = _configuration.TypeConversionFunction(typeData.Symbol);
				if (typeData.Conversion == TypeConversion.Ignore)
				{
					continue;
				}
				
				foreach (var methodNode in typeNode
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>())
				{
					var methodData = documentData.GetOrCreateMethodData(methodNode);
					await AnalyzeMethodData(methodData).ConfigureAwait(false);
				}
			}
		}

		private async Task ScanDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas()
				.Where(o => o.Conversion != TypeConversion.Ignore))
			{
				// If the type have to be defined as a new type then we need to find all references to that type 
				if (typeData.Conversion == TypeConversion.NewType)
				{
					await ScanForTypeReferences(typeData).ConfigureAwait(false);
				}

				if (_configuration.ScanForMissingAsyncMembers)
				{
					ScanForTypeMissingAsyncMethods(typeData);
				}

				foreach (var methodData in typeData.MethodData.Values
					.Where(o => o.Conversion != MethodConversion.Ignore))
				{
					await ScanMethodData(methodData);
				}
			}
		}

		//TODO: remove
		private readonly ConcurrentSet<IMethodSymbol> _scannedMethodSymbols = new ConcurrentSet<IMethodSymbol>();

		private async Task ScanMethodData(MethodData methodData, int depth = 0)
		{
			if (_scannedMethodSymbols.Contains(methodData.Symbol.OriginalDefinition))
			{
				return;
			}
			_scannedMethodSymbols.Add(methodData.Symbol.OriginalDefinition);
			SyntaxReference syntax;
			var bodyScanMethodDatas = new HashSet<MethodData> {methodData};
			var referenceScanMethods = new HashSet<IMethodSymbol>();

			var interfaceMethods = methodData.ImplementedInterfaces.ToImmutableHashSet();
			if (methodData.InterfaceMethod)
			{
				interfaceMethods = interfaceMethods.Add(methodData.Symbol);
			}
			// Get and save all interface implementations
			foreach (var interfaceMethod in interfaceMethods)
			{
				if (_scannedMethodSymbols.Contains(interfaceMethod))
				{
					continue;
				}
				_scannedMethodSymbols.Add(interfaceMethod);

				referenceScanMethods.Add(interfaceMethod);

				syntax = interfaceMethod.DeclaringSyntaxReferences.Single();
				var document = _solution.GetDocument(syntax.SyntaxTree);
				if (!CanProcessDocument(document))
				{
					continue;
				}
				var documentData = ProjectData.GetDocumentData(document);
				var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
				var interfaceMethodData = documentData.GetOrCreateMethodData(methodNode);

				var implementations = await SymbolFinder.FindImplementationsAsync(
					interfaceMethod.OriginalDefinition, _solution, _analyzeProjects)
					.ConfigureAwait(false);
				foreach (var implementation in implementations.OfType<IMethodSymbol>())
				{
					syntax = implementation.DeclaringSyntaxReferences.Single();
					documentData = ProjectData.GetDocumentData(document);
					methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var implMethodData = documentData.GetOrCreateMethodData(methodNode);

					interfaceMethodData.RelatedMethods.TryAdd(implMethodData);
					implMethodData.RelatedMethods.TryAdd(interfaceMethodData);

					bodyScanMethodDatas.Add(implMethodData);
				}
			}

			MethodData baseMethodData = null;
			IMethodSymbol baseMethodSymbol;
			if (methodData.BaseOverriddenMethod?.DeclaringSyntaxReferences.Any() == true)
			{
				baseMethodSymbol = methodData.BaseOverriddenMethod;
			}
			else
			{
				baseMethodSymbol = methodData.Symbol.IsVirtual || methodData.Symbol.IsAbstract
					? methodData.Symbol
					: null;
				baseMethodData = methodData;
			}

			// Get and save all derived methods
			if (baseMethodSymbol != null && !_scannedMethodSymbols.Contains(baseMethodSymbol))
			{
				_scannedMethodSymbols.Add(baseMethodSymbol);

				referenceScanMethods.Add(baseMethodSymbol);

				if (baseMethodData == null)
				{
					syntax = baseMethodSymbol.DeclaringSyntaxReferences.Single();
					var document = _solution.GetDocument(syntax.SyntaxTree);
					if (CanProcessDocument(document))
					{
						var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
						baseMethodData = ProjectData.GetDocumentData(document).GetOrCreateMethodData(methodNode);
					}
				}

				if (baseMethodData != null)
				{
					bodyScanMethodDatas.Add(baseMethodData);
				}

				var overrides = await SymbolFinder.FindOverridesAsync(baseMethodSymbol.OriginalDefinition, 
					_solution, _analyzeProjects).ConfigureAwait(false);
				foreach (var overrideMethod in overrides.OfType<IMethodSymbol>())
				{
					syntax = overrideMethod.DeclaringSyntaxReferences.Single();
					var document = _solution.GetDocument(syntax.SyntaxTree);
					if (!CanProcessDocument(document))
					{
						continue;
					}
					var documentData = ProjectData.GetDocumentData(document);
					var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var overrideMethodData = documentData.GetOrCreateMethodData(methodNode);

					if (baseMethodData != null)
					{
						overrideMethodData.RelatedMethods.TryAdd(baseMethodData);
						baseMethodData.RelatedMethods.TryAdd(overrideMethodData);
					}
					else
					{
						overrideMethodData.ExternalRelatedMethods.TryAdd(baseMethodSymbol);
					}

					if (!overrideMethod.IsAbstract)
					{
						bodyScanMethodDatas.Add(overrideMethodData);
					}
				}
			}

			if (baseMethodSymbol == null && !interfaceMethods.Any()) //TODO: what about hiding methods
			{
				referenceScanMethods.Add(methodData.Symbol);
			}

			if (_configuration.ScanMethodBody)
			{
				var asyncCounterparts = new List<AsyncCounterpartMethod>();
				foreach (var mData in bodyScanMethodDatas)
				{
					asyncCounterparts.AddRange(await FindAsyncCounterpartMethodsWhitinMethod(mData).ConfigureAwait(false));
				}
				foreach (var group in asyncCounterparts.GroupBy(o => o.MethodSymbol))
				{
					await ScanAllMethodReferenceLocations(group.Key, depth).ConfigureAwait(false);
				}
			}

			foreach (var methodToScan in referenceScanMethods)
			{
				await ScanAllMethodReferenceLocations(methodToScan, depth).ConfigureAwait(false);
			}
		}

		private async Task<bool> SmartAnalyzeMethod(MethodData methodData)
		{
			switch (methodData.Conversion)
			{
				case MethodConversion.Ignore:
					return false;
				case MethodConversion.ToAsync:
					return true;
				default:
					var asyncConterParts = await FindAsyncCounterpartMethodsWhitinMethod(methodData).ConfigureAwait(false);
					//TODO: check if the invocations are not invocated in a delegate
					return asyncConterParts.Any();
			}
		}

		private bool CanProcessDocument(Document doc)
		{
			if (doc.Project != ProjectData.Project)
			{
				return false;
			}
			return _analyzeDocuments.Contains(doc);
		}

		private async Task AnalyzeMethodData(MethodData methodData)
		{
			if (methodData.SymbolAnalyzed)
			{
				return;
			}
			methodData.SymbolAnalyzed = true;

			var methodSymbol = methodData.Symbol;
			methodData.Conversion = _configuration.MethodConversionFunction(methodSymbol);
			if (methodData.Conversion == MethodConversion.Ignore)
			{
				Logger.Debug($"Method {methodSymbol} will be ignored because of MethodConversionFunction");
				return;
			}
			if (methodData.Conversion == MethodConversion.Smart && !await SmartAnalyzeMethod(methodData).ConfigureAwait(false))
			{
				methodData.Conversion = MethodConversion.Unknown; //TODO: is needed?
			}

			var forceAsync = methodData.Conversion == MethodConversion.ToAsync;
			if (methodSymbol.IsAsync || methodSymbol.Name.EndsWith("Async"))
			{
				if (forceAsync)
				{
					Logger.Warn($"Symbol {methodSymbol} is already async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				methodData.IsAsync = true;
				return;
			}
			if (methodSymbol.MethodKind != MethodKind.Ordinary && methodSymbol.MethodKind != MethodKind.ExplicitInterfaceImplementation)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is a {methodSymbol.MethodKind} and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			if (methodSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} has out parameters and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault() == null)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is external and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			// Check if explicitly implements external interfaces
			if (methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				foreach (var interfaceMember in methodSymbol.ExplicitInterfaceImplementations)
				{
					if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
					{
						methodData.ExternalRelatedMethods.TryAdd(interfaceMember);

						// Check if the interface member has an async counterpart
						var asyncConterPart = interfaceMember.ContainingType.GetMembers()
							.OfType<IMethodSymbol>()
							.Where(o => o.Name == methodSymbol.Name + "Async")
							.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
						
						if (asyncConterPart == null)
						{
							Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
							methodData.Conversion = MethodConversion.Ignore;
							return;
						}
						methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
					}
					else
					{
						methodData.ImplementedInterfaces.TryAdd(interfaceMember);
					}
					//var syntax = interfaceMember.DeclaringSyntaxReferences.FirstOrDefault();
					//if (!CanProcessSyntaxReference(syntax))
					//{
					//	continue;
					//}
					
				}
			}

			// Check if the method is overriding an external method
			var overridenMethod = methodSymbol.OverriddenMethod;
			while (overridenMethod != null)
			{
				if (methodSymbol.ContainingAssembly.Name != overridenMethod.ContainingAssembly.Name)
				{
					methodData.ExternalRelatedMethods.TryAdd(overridenMethod);
					// Check if the external member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = overridenMethod.ContainingType.GetMembers()
						.OfType<IMethodSymbol>()
						.Where(o => o.Name == methodSymbol.Name + "Async" && !o.IsSealed && (o.IsVirtual || o.IsAbstract || o.IsOverride))
						.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						Logger.Warn(
							$"Method {methodSymbol} overrides an external method {overridenMethod} that has not an async counterpart... method will not be converted");
						methodData.Conversion = MethodConversion.Ignore;
						return;
						//if (!asyncMethods.Any() || (asyncMethods.Any() && !overridenMethod.IsOverride && !overridenMethod.IsVirtual))
						//{
						//	Logger.Warn($"Method {methodSymbol} overrides an external method {overridenMethod} and cannot be made async");
						//	return MethodSymbolAnalyzeResult.Invalid;
						//}
					}
					methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
				}
				else
				{
					methodData.OverridenMethods.TryAdd(overridenMethod);
				}
				//var syntax = overridenMethod.DeclaringSyntaxReferences.SingleOrDefault();
				//else if (CanProcessSyntaxReference(syntax))
				//{
				//	methodData.OverridenMethods.TryAdd(overridenMethod);
				//}
				if (overridenMethod.OverriddenMethod != null)
				{
					overridenMethod = overridenMethod.OverriddenMethod;
				}
				else
				{
					break;
				}
			}
			methodData.BaseOverriddenMethod = overridenMethod;

			// Check if the method is implementing an external interface, if true skip as we cannot modify externals
			// FindImplementationForInterfaceMember will find the first implementation method starting from the deepest base class
			var type = methodSymbol.ContainingType;
			foreach (var interfaceMember in type.AllInterfaces
												.SelectMany(
													o => o.GetMembers(methodSymbol.Name)
														  .Where(
															  m =>
															  {
																  // Find out if the method implements the interface member or an override 
																  // method that implements it
																  var impl = type.FindImplementationForInterfaceMember(m);
																  return methodSymbol.Equals(impl) || methodData.OverridenMethods.Any(ov => ov.Equals(impl));
															  }
															))
														  .OfType<IMethodSymbol>())
			{
				if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
				{
					methodData.ExternalRelatedMethods.TryAdd(interfaceMember);

					// Check if the member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = interfaceMember.ContainingType.GetMembers()
						.OfType<IMethodSymbol>()
						.Where(o => o.Name == methodSymbol.Name + "Async")
						.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
						methodData.Conversion = MethodConversion.Ignore;
						return;
					}
					methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
				}
				else
				{
					methodData.ImplementedInterfaces.TryAdd(interfaceMember);
				}
				//var syntax = interfaceMember.DeclaringSyntaxReferences.SingleOrDefault();
				//if (!CanProcessSyntaxReference(syntax))
				//{
				//	continue;
				//}
				
			}

			// Verify if there is already an async counterpart for this method
			IMethodSymbol asyncCounterpart = null;
			if (_configuration.FindAsyncCounterpartDelegates.Any())
			{
				foreach (var findAsyncConterpart in _configuration.FindAsyncCounterpartDelegates)
				{
					asyncCounterpart = await findAsyncConterpart(ProjectData.Project, methodSymbol, true).ConfigureAwait(false);
					if (asyncCounterpart != null)
					{
						break;
					}
				}
			}
			else
			{
				asyncCounterpart = methodSymbol.GetAsyncCounterpart();
			}
			if (asyncCounterpart != null)
			{
				Logger.Debug($"Method {methodSymbol} has already an async counterpart {asyncCounterpart}");
				methodData.AsyncCounterpartSymbol = asyncCounterpart;
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}
		}

		#region ScanAllMethodReferenceLocations

		private readonly ConcurrentSet<IMethodSymbol> _scannedMethodReferenceSymbols = new ConcurrentSet<IMethodSymbol>();

		private async Task ScanAllMethodReferenceLocations(IMethodSymbol methodSymbol, int depth = 0)
		{
			if (_scannedMethodReferenceSymbols.Contains(methodSymbol.OriginalDefinition))
			{
				return;
			}
			_scannedMethodReferenceSymbols.TryAdd(methodSymbol.OriginalDefinition);

			var references = await SymbolFinder.FindReferencesAsync(methodSymbol.OriginalDefinition,
				_solution, _analyzeDocuments).ConfigureAwait(false);

			depth++;
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				if (refLocation.Document.Project != ProjectData.Project)
				{
					throw new InvalidOperationException($"Reference {refLocation} is referencing a symbol from another project");
				}

				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				if (documentData == null)
				{
					continue;
				}
				var symbol = documentData.GetEnclosingSymbol(refLocation);
				if (symbol == null)
				{
					Logger.Debug($"Symbol not found for reference ${refLocation}");
					continue;
				}

				var refMethodSymbol = symbol as IMethodSymbol;
				if (refMethodSymbol == null)
				{
					continue;
				}
				var methodData = await documentData.GetMethodData(refMethodSymbol).ConfigureAwait(false);
				if (methodData == null)
				{
					continue;
				}
				// Save the reference as it can be made async
				if (!methodData.MethodReferences.TryAdd(refLocation))
				{
					continue; // Reference already processed
				}

				// Find the real method on that reference as FindReferencesAsync will also find references to base and interface methods
				var nameNode = methodData.Node.DescendantNodes()
							   .OfType<SimpleNameSyntax>()
							   .First(
								   o =>
								   {
									   if (o.IsKind(SyntaxKind.GenericName))
									   {
										   return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span ==
												  refLocation.Location.SourceSpan;
									   }
									   return o.Span == refLocation.Location.SourceSpan;
								   });
				var invokedSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				var invokedMethodDocData = invokedSymbol.DeclaringSyntaxReferences
																  .Select(o => ProjectData.GetDocumentData(o))
																  .SingleOrDefault();
				if (invokedMethodDocData != null)
				{
					var invokedMethodData = await invokedMethodDocData.GetMethodData(invokedSymbol).ConfigureAwait(false);
					invokedMethodData?.InvokedBy.Add(methodData);
				}
				//await ScanMethodData(methodData, depth).ConfigureAwait(false);
			}
		}

		#endregion

		private async Task<List<AsyncCounterpartMethod>> FindAsyncCounterpartMethodsWhitinMethod(MethodData methodData)
		{
			var result = new List<AsyncCounterpartMethod>();
			if (methodData.Node.Body == null)
			{
				return result;
			}
			var documentData = methodData.TypeData.NamespaceData.DocumentData;
			var semanticModel = documentData.SemanticModel;
			foreach (var invocation in methodData.Node.Body.DescendantNodes()
										   .OfType<InvocationExpressionSyntax>())
			{
				var methodSymbol = ModelExtensions.GetSymbolInfo(semanticModel, invocation.Expression).Symbol as IMethodSymbol;
				if (methodSymbol == null)
				{
					continue;
				}
				methodSymbol = methodSymbol.OriginalDefinition;
				IMethodSymbol asyncMethodSymbol;
				if (!_methodAsyncConterparts.TryGetValue(methodSymbol, out asyncMethodSymbol))
				{
					if (_configuration.FindAsyncCounterpartDelegates.Any())
					{
						foreach (var findAsyncConterpart in _configuration.FindAsyncCounterpartDelegates)
						{
							asyncMethodSymbol = await findAsyncConterpart(documentData.ProjectData.Project, methodSymbol.OriginalDefinition, true).ConfigureAwait(false);
							if (asyncMethodSymbol != null)
							{
								break;
							}
						}
					}
					else
					{
						asyncMethodSymbol = methodSymbol.GetAsyncCounterpart(true);
					}
					var value = asyncMethodSymbol?.OriginalDefinition;
					_methodAsyncConterparts.AddOrUpdate(methodSymbol, value, (k, v) => value);
				}

				if (asyncMethodSymbol == null)
				{
					continue;
				}

				result.Add(new AsyncCounterpartMethod
				{
					MethodSymbol = methodSymbol.OriginalDefinition,
					AsyncMethodSymbol = asyncMethodSymbol.OriginalDefinition,
					MethodNode = invocation.Expression
				});
			}
			return result;
		}

		private void ScanForTypeMissingAsyncMethods(TypeData typeData)
		{
			var documentData = typeData.NamespaceData.DocumentData;
			var members = typeData.Node
				.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Select(o => new { Node = o, Symbol = documentData.SemanticModel.GetDeclaredSymbol(o)})
				.ToLookup(o =>
					o.Symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation
						? o.Symbol.Name.Split('.').Last()
						: o.Symbol.Name);
			//var methodDatas = new List<MethodData>();

			foreach (var asyncMember in typeData.Symbol.AllInterfaces
												  .SelectMany(o => o.GetMembers().OfType<IMethodSymbol>()
												  .Where(m => m.Name.EndsWith("Async"))))
			{
				// Skip if there is already an implementation defined
				var impl = typeData.Symbol.FindImplementationForInterfaceMember(asyncMember);
				if (impl != null)
				{
					continue;
				}
				var nonAsyncName = asyncMember.Name.Remove(asyncMember.Name.LastIndexOf("Async", StringComparison.InvariantCulture));
				if (!members.Contains(nonAsyncName))
				{
					continue;
				}
				var nonAsyncMember = members[nonAsyncName].First(o => o.Symbol.HaveSameParameters(asyncMember));
				var methodData = documentData.GetOrCreateMethodData(nonAsyncMember.Node);
				methodData.Conversion = MethodConversion.ToAsync;
				//methodDatas.Add(methodData);
			}

			// Find all abstract non implemented async methods. Descend base types until we find a non abstract one.
			var baseType = typeData.Symbol.BaseType;
			while (baseType != null)
			{
				if (!baseType.IsAbstract)
				{
					break;
				}
				foreach (var asyncMember in baseType.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(o => o.IsAbstract && o.Name.EndsWith("Async")))
				{
					var nonAsyncName = asyncMember.Name.Remove(asyncMember.Name.LastIndexOf("Async", StringComparison.InvariantCulture));
					if (!members.Contains(nonAsyncName))
					{
						Logger.Info($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var nonAsyncMember = members[nonAsyncName].FirstOrDefault(o => o.Symbol.HaveSameParameters(asyncMember));
					if (nonAsyncMember == null)
					{
						Logger.Info($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var methodData = documentData.GetOrCreateMethodData(nonAsyncMember.Node);
					methodData.Conversion = MethodConversion.ToAsync;
					//methodDatas.Add(methodData);
				}
				baseType = baseType.BaseType;
			}

			//if (_configuration.ScanMethodBody)
			//{
			//	var asnycCounterparts = new List<AsyncCounterpartMethod>();
			//	foreach (var methodData in methodDatas)
			//	{
			//		asnycCounterparts.AddRange(await FindAsyncCounterpartMethodsWhitinMethod(methodData).ConfigureAwait(false));
			//	}
			//	foreach (var group in asnycCounterparts.GroupBy(o => o.MethodSymbol))
			//	{
			//		await ScanAllMethodReferenceLocations(group.Key).ConfigureAwait(false);
			//	}
			//}
		}

		/// <summary>
		/// When a type needs to be defined as a new type we need to find all references to them.
		/// Reference can point to a variable, field, base type, argument definition
		/// </summary>
		private async Task ScanForTypeReferences(TypeData typeData)
		{
			// References for ctor of the type and the type itself wont have any locations
			var references = await SymbolFinder.FindReferencesAsync(typeData.Symbol, _solution, _analyzeDocuments).ConfigureAwait(false);
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				if (!typeData.SelfReferences.TryAdd(refLocation))
				{
					continue; // Reference already processed
				}

				// We need to find the type where the reference location is
				var node = documentData.RootNode.DescendantNodes(descendIntoTrivia: true)
					.First(
						o =>
						{
							if (o.IsKind(SyntaxKind.GenericName))
							{
								return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span ==
									   refLocation.Location.SourceSpan;
							}
							return o.Span == refLocation.Location.SourceSpan;
						});

				var methodNode = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
				if (methodNode != null)
				{
					var methodData = documentData.GetOrCreateMethodData(methodNode);
					methodData.TypeReferences.TryAdd(refLocation);
				}
				else
				{
					var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
					if (type != null)
					{
						var refTypeData = documentData.GetOrCreateTypeData(type);
						refTypeData.TypeReferences.TryAdd(refLocation);
					}
					else // Can happen when declaring a Name in a using statement
					{
						var namespaceNode = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
						var namespaceData = documentData.GetNamespaceData(namespaceNode, true);
						namespaceData.TypeReferences.TryAdd(refLocation);
					}
				}
			}
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


	public class AsyncCodeGenerator
	{
		public async Task GenerateAsync(IAsyncCodeConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			var conf = configuration.Build();

			foreach (var config in conf.SolutionConfigurations)
			{
				var solutionData = await CreateSolutionData(config).ConfigureAwait(false);
				foreach (var projectData in solutionData.ProjectData.Values)
				{
					await AnalyzeProject(projectData).ConfigureAwait(false);
				}
			}

			//conf.SolutionConfigurations.First().ProjectConfigurations.First().TransformConfiguration.
		}

		private Task AnalyzeProject(ProjectData projectData)
		{
			var analyzer = new ProjectAnalyzer(projectData);
			return analyzer.Analyze();
		}


		private async Task<SolutionData> CreateSolutionData(SolutionConfiguration configuration)
		{
			var workspace = MSBuildWorkspace.Create();
			var solution = await workspace.OpenSolutionAsync(configuration.Path).ConfigureAwait(false);
			var solutionData = new SolutionData(solution, workspace, configuration);

			var projectConfigs = configuration.ProjectConfigurations.ToDictionary(o => o.Name);
			foreach (var project in solution.Projects.Where(o => projectConfigs.ContainsKey(o.Name)))
			{
				var config = projectConfigs[project.Name];
				var projectData = new ProjectData(solutionData, project.Id, config);
				RemoveGeneratedDocuments(projectData);
				solutionData.ProjectData.AddOrUpdate(project.Id, projectData, (id, data) => projectData);
			}
			return solutionData;
		}

		private void RemoveGeneratedDocuments(ProjectData projectData)
		{
			var project = projectData.Project;
			var asyncFolder = projectData.Configuration.TransformConfiguration.AsyncFolder;
			if (string.IsNullOrEmpty(asyncFolder))
			{
				return;
			}
			var asyncProjectFolder = Path.Combine(projectData.DirectoryPath, asyncFolder) + @"\";
			// remove all generated documents
			var toRemove = project.Documents.Where(o => o.FilePath.StartsWith(asyncProjectFolder)).Select(doc => doc.Id).ToList();
			foreach (var docId in toRemove)
			{
				project = project.RemoveDocument(docId);
			}
			projectData.Project = project;
		}
	}
}
