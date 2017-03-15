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
	public class MethodData
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node)
		{
			TypeData = typeData;
			Symbol = symbol;
			Node = node;
		}

		/// <summary>
		/// Contains references of types that are used inside this method
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<ReferenceLocation> MethodReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public MethodConversion Conversion { get; internal set; }

		public TypeData TypeData { get; }

		public IMethodSymbol Symbol { get; }

		public MethodDeclarationSyntax Node { get; }
	}

	public class TypeData
	{
		public TypeData(NamespaceData namespaceData, INamedTypeSymbol symbol, TypeDeclarationSyntax node, TypeData parentTypeData = null)
		{
			NamespaceData = namespaceData;
			ParentTypeData = parentTypeData;
			Symbol = symbol;
			Node = node;
		}

		/// <summary>
		/// Contains references of types that are used inside this type
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// Contains references to itself
		/// </summary>
		public ConcurrentSet<ReferenceLocation> SelfReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public TypeData ParentTypeData { get; }

		public NamespaceData NamespaceData { get; }

		public INamedTypeSymbol Symbol { get; }

		public TypeDeclarationSyntax Node { get; }

		public TypeConversion Conversion { get; internal set; }

		public ConcurrentDictionary<MethodDeclarationSyntax, MethodData> MethodData { get; } = new ConcurrentDictionary<MethodDeclarationSyntax, MethodData>();

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> NestedTypeData { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		public MethodData GetMethodData(MethodDeclarationSyntax methodNode, bool create = false)
		{
			var methodSymbol = NamespaceData.DocumentData.SemanticModel.GetDeclaredSymbol(methodNode);
			return GetMethodData(methodSymbol, methodNode, create);
		}

		public MethodData GetMethodData(IMethodSymbol symbol, bool create = false)
		{
			var location = symbol.Locations.Single(o => o.SourceTree.FilePath == Node.SyntaxTree.FilePath);
			var memberNode = Node.DescendantNodes()
									 .OfType<MethodDeclarationSyntax>()
									 .First(o => o.ChildTokens().SingleOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == location.SourceSpan);
			return GetMethodData(symbol, memberNode, create);
		}

		public MethodData GetMethodData(IMethodSymbol methodSymbol, MethodDeclarationSyntax methodNode, bool create = false)
		{
			MethodData methodData;
			if (MethodData.TryGetValue(methodNode, out methodData))
			{
				return methodData;
			}
			return !create ? null : MethodData.GetOrAdd(methodNode, syntax => new MethodData(this, methodSymbol, methodNode));
		}
	}

	public class NamespaceData
	{
		public NamespaceData(DocumentData documentData, INamespaceSymbol symbol, NamespaceDeclarationSyntax node)
		{
			DocumentData = documentData;
			Symbol = symbol;
			Node = node;
		}

		/// <summary>
		/// Contains references of types that are used inside this namespace (alias to a type with a using statement)
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public DocumentData DocumentData { get; }

		public INamespaceSymbol Symbol { get; }

		public NamespaceDeclarationSyntax Node { get; }

		public bool IsGlobal => Node == null;

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> TypeData { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		public TypeData GetTypeData(TypeDeclarationSyntax node, bool create = false)
		{
			var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
			return GetTypeData(typeSymbol, create);
		}

		public TypeData GetTypeData(MethodDeclarationSyntax node, bool create = false)
		{
			var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node).ContainingType;
			return GetTypeData(typeSymbol, create);
		}

		public TypeData GetTypeData(IMethodSymbol symbol, bool create = false)
		{
			return GetTypeData(symbol.ContainingType, create);
		}

		public TypeData GetTypeData(INamedTypeSymbol type, bool create = false)
		{
			var nestedTypes = new Stack<INamedTypeSymbol>();
			while (type != null)
			{
				nestedTypes.Push(type);
				type = type.ContainingType;
			}
			TypeData currentTypeData = null;
			var path = DocumentData.FilePath;
			while (nestedTypes.Count > 0)
			{
				var typeSymbol = nestedTypes.Pop().OriginalDefinition;
				var location = typeSymbol.Locations.Single(o => o.SourceTree.FilePath == path);
				var namespaceNode = Node ?? (SyntaxNode)DocumentData.RootNode; // Global namespace
				var node = namespaceNode.DescendantNodes()
							   .OfType<TypeDeclarationSyntax>()
							   .First(o => o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == location.SourceSpan);

				var typeDataDict = currentTypeData?.NestedTypeData ?? TypeData;
				TypeData typeData;
				if (typeDataDict.TryGetValue(node, out typeData))
				{
					currentTypeData = typeData;
					continue;
				}
				if (!create)
				{
					return null;
				}
				currentTypeData = typeDataDict.GetOrAdd(node, syntax => new TypeData(this, typeSymbol, node, currentTypeData));
			}
			return currentTypeData;
		}
	}

	public class DocumentData
	{
		public DocumentData(ProjectData projectData, Document document, CompilationUnitSyntax rootNode, SemanticModel semanticModel)
		{
			ProjectData = projectData;
			Document = document;
			RootNode = rootNode;
			SemanticModel = semanticModel;
			GlobalNamespaceData = new NamespaceData(this, SemanticModel.Compilation.GlobalNamespace, null);
		}

		public Document Document { get; }

		public string FilePath => Document.FilePath;

		public ProjectData ProjectData { get; }

		public CompilationUnitSyntax RootNode { get; }

		public SemanticModel SemanticModel { get; }

		public NamespaceData GlobalNamespaceData { get; }

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> NamespaceData { get; } = new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		public MethodData GetOrCreateMethodData(MethodDeclarationSyntax node)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true).GetMethodData(node, true);
		}
		
		public MethodData GetOrCreateMethodData(IMethodSymbol symbol)
		{
			return GetNamespaceData(symbol, true).GetTypeData(symbol, true).GetMethodData(symbol, true);
		}

		public TypeData GetOrCreateTypeData(TypeDeclarationSyntax node)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true);
		}

		public NamespaceData GetNamespaceData(ISymbol symbol, bool create = false)
		{
			var namespaceSymbol = symbol.ContainingNamespace;
			if (namespaceSymbol.IsGlobalNamespace)
			{
				return GlobalNamespaceData;
			}

			var location = namespaceSymbol.Locations.Single(o => o.SourceTree.FilePath == FilePath);
			var node = RootNode.DescendantNodes()
							   .OfType<NamespaceDeclarationSyntax>()
							   .FirstOrDefault(
								   o =>
								   {
									   var identifier = o.ChildNodes().OfType<IdentifierNameSyntax>().SingleOrDefault();
									   if (identifier != null)
									   {
										   return identifier.Span == location.SourceSpan;
									   }
									   return o.ChildNodes().OfType<QualifiedNameSyntax>().Single().Right.Span == location.SourceSpan;
								   });
			if (node == null) //TODO: location.SourceSpan.Start == 0 -> a bug perhaps ???
			{
				node = RootNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single(o => o.FullSpan.End == location.SourceSpan.End);
			}
			return GetNamespaceData(node, namespaceSymbol, create);
		}

		public NamespaceData GetNamespaceData(SyntaxNode node, bool create = false)
		{
			if (node == null)
			{
				return GlobalNamespaceData;
			}
			var namespaceNode = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
			if (namespaceNode == null)
			{
				return GlobalNamespaceData;
			}
			var namespaceSymbol = SemanticModel.GetDeclaredSymbol(namespaceNode);
			return GetNamespaceData(namespaceNode, namespaceSymbol, create);
		}

		private NamespaceData GetNamespaceData(NamespaceDeclarationSyntax namespaceNode, INamespaceSymbol namespaceSymbol, bool create = false)
		{
			NamespaceData namespaceData;
			if (NamespaceData.TryGetValue(namespaceNode, out namespaceData))
			{
				return namespaceData;
			}
			return !create ? null : NamespaceData.GetOrAdd(namespaceNode, syntax => new NamespaceData(this, namespaceSymbol, namespaceNode));
		}

		// TODO: DEBUG
		public ISymbol GetEnclosingSymbol(ReferenceLocation reference)
		{
			var enclosingSymbol = SemanticModel.GetEnclosingSymbol(reference.Location.SourceSpan.Start);

			for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
			{
				if (current.Kind == SymbolKind.Field)
				{
					return current;
				}

				if (current.Kind == SymbolKind.Property)
				{
					return current;
				}

				if (current.Kind == SymbolKind.Method)
				{
					var method = (IMethodSymbol)current;
					if (method.IsAccessor())
					{
						return method.AssociatedSymbol;
					}

					if (method.MethodKind != MethodKind.AnonymousFunction)
					{
						return method;
					}
				}
			}
			// reference to a cref
			return null;
		}
	}

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
		private ProjectAnalyzeConfiguration _configuration;
		private readonly ConcurrentDictionary<IMethodSymbol, IMethodSymbol> _methodAsyncConterparts = new ConcurrentDictionary<IMethodSymbol, IMethodSymbol>();

		public ProjectAnalyzer(ProjectData projectData)
		{
			ProjectData = projectData;
		}

		public ProjectData ProjectData { get; }

		public async Task Analyze()
		{
			Setup();
			var documentData = await Task.WhenAll(_analyzeDocuments
				.Select(o => ProjectData.CreateDocumentData(o)))
				.ConfigureAwait(false);

			await Task.WhenAll(documentData
				.Select(AnalyzeDocumentData))
				.ConfigureAwait(false);
		}


		public async Task AnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeNode in documentData.RootNode
				.DescendantNodes()
				.OfType<TypeDeclarationSyntax>())
			{
				var typeData = documentData.GetOrCreateTypeData(typeNode);
				// Not needed as we have TypeConversionFunction
				//if (!_configuration.TypeSelectionPredicate(typeData.Symbol))
				//{
				//	continue;
				//}
				typeData.Conversion = _configuration.TypeConversionFunction(typeData.Symbol);
				if (typeData.Conversion == TypeConversion.Ignore)
				{
					continue;
				}
				// If the type have to be defined as a new type then we need to find all references to that type 
				// TODO: move this to post analyze?
				if (typeData.Conversion == TypeConversion.NewType)
				{
					await ScanForTypeReferences(typeData).ConfigureAwait(false);
				}

				if (_configuration.ScanForMissingAsyncMembers)
				{
					await ScanForTypeMissingAsyncMethods(typeData).ConfigureAwait(false);
				}

				foreach (var methodNode in typeNode
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>())
				{
					var methodData = 

				}

			}
		}

		private bool CanProcessSyntaxReference(SyntaxReference syntax)
		{
			return CanProcessDocument(ProjectData.Project.Solution.GetDocument(syntax.SyntaxTree));
		}

		private bool CanProcessDocument(Document doc)
		{
			if (doc.Project != ProjectData.Project)
			{
				return false;
			}
			return _analyzeDocuments.Contains(doc);
		}

		private class MethodSymbolAnalyzeResult
		{
			public static readonly MethodSymbolAnalyzeResult Invalid = new MethodSymbolAnalyzeResult();

			public HashSet<IMethodSymbol> InterfaceMethods { get; set; }

			public HashSet<IMethodSymbol> AsyncInterfaceMethods { get; set; }

			public HashSet<IMethodSymbol> OverriddenMethods { get; set; }

			public IMethodSymbol BaseOverriddenMethod { get; set; }

			public IMethodSymbol MethodSymbol { get; set; }

			public bool IsValid { get; set; }
		}

		private readonly ConcurrentDictionary<IMethodSymbol, MethodSymbolAnalyzeResult> _cachedMethodSymbolInfos = new ConcurrentDictionary<IMethodSymbol, MethodSymbolAnalyzeResult>();

		private async Task<MethodSymbolAnalyzeResult> AnalyzeMethodSymbol(IMethodSymbol methodSymbol, bool forceAsync)
		{
			MethodSymbolAnalyzeResult result;
			if (_cachedMethodSymbolInfos.TryGetValue(methodSymbol.OriginalDefinition, out result))
			{
				return result;
			}

			if (methodSymbol.Name.EndsWith("Async"))
			{
				if (forceAsync)
				{
					Logger.Warn($"Symbol {methodSymbol} is already async");
				}
				return MethodSymbolAnalyzeResult.Invalid;
			}
			if (methodSymbol.MethodKind != MethodKind.Ordinary && methodSymbol.MethodKind != MethodKind.ExplicitInterfaceImplementation)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is a {methodSymbol.MethodKind} and cannot be made async");
				}
				return MethodSymbolAnalyzeResult.Invalid;
			}

			if (methodSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} has out parameters and cannot be made async");
				}
				return MethodSymbolAnalyzeResult.Invalid;
			}

			if (methodSymbol.DeclaringSyntaxReferences.SingleOrDefault() == null)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is external and cannot be made async");
				}
				return MethodSymbolAnalyzeResult.Invalid;
			}

			var interfaceMethods = new HashSet<IMethodSymbol>();
			var asyncMethods = new HashSet<IMethodSymbol>();
			// Check if explicitly implements external interfaces
			if (methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				foreach (var interfaceMember in methodSymbol.ExplicitInterfaceImplementations)
				{
					var syntax = interfaceMember.DeclaringSyntaxReferences.SingleOrDefault();
					if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
					{
						// Check if the member has an async counterpart that is not implemented in the current type (missing member)
						var asyncConterPart = interfaceMember.ContainingType.GetMembers()
															 .OfType<IMethodSymbol>()
															 .Where(o => o.Name == methodSymbol.Name + "Async")
															 .SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
						if (asyncConterPart == null)
						{
							Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
							return MethodSymbolAnalyzeResult.Invalid;
						}
						asyncMethods.Add(asyncConterPart);
					}
					if (!CanProcessSyntaxReference(syntax))
					{
						continue;
					}
					interfaceMethods.Add(interfaceMember.OriginalDefinition);
				}
			}

			// Check if the method is overriding an external method
			var overridenMethod = methodSymbol.OverriddenMethod;
			var overrrides = new HashSet<IMethodSymbol>();
			while (overridenMethod != null)
			{
				var syntax = overridenMethod.DeclaringSyntaxReferences.SingleOrDefault();
				if (methodSymbol.ContainingAssembly.Name != overridenMethod.ContainingAssembly.Name)
				{
					// Check if the member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = overridenMethod.ContainingType.GetMembers()
														 .OfType<IMethodSymbol>()
														 .Where(o => o.Name == methodSymbol.Name + "Async" && !o.IsSealed && (o.IsVirtual || o.IsAbstract || o.IsOverride))
														 .SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						if (!asyncMethods.Any() || (asyncMethods.Any() && !overridenMethod.IsOverride && !overridenMethod.IsVirtual))
						{
							Logger.Warn($"Method {methodSymbol} overrides an external method {overridenMethod} and cannot be made async");
							return MethodSymbolAnalyzeResult.Invalid;
						}
					}
					else
					{
						asyncMethods.Add(asyncConterPart);
					}
				}
				else if (CanProcessSyntaxReference(syntax))
				{
					overrrides.Add(overridenMethod.OriginalDefinition);
				}

				if (overridenMethod.OverriddenMethod != null)
				{
					overridenMethod = overridenMethod.OverriddenMethod;
				}
				else
				{
					break;
				}
			}

			// Check if the method is implementing an external interface, if true skip as we cannot modify externals
			// FindImplementationForInterfaceMember will find the first implementation method starting from the deepest base class
			var type = methodSymbol.ContainingType;
			foreach (var interfaceMember in type.AllInterfaces
												.SelectMany(
													o => o.GetMembers(methodSymbol.Name)
														  .Where(
															  m =>
															  {
																  // find out if the method implements the interface member or an override 
																  // method that implements it
																  var impl = type.FindImplementationForInterfaceMember(m);
																  return methodSymbol.Equals(impl) || overrrides.Any(ov => ov.Equals(impl));
															  }
															))
														  .OfType<IMethodSymbol>())
			{
				var syntax = interfaceMember.DeclaringSyntaxReferences.SingleOrDefault();
				if (syntax == null || methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
				{
					// check if the member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = interfaceMember.ContainingType.GetMembers()
														 .OfType<IMethodSymbol>()
														 .Where(o => o.Name == methodSymbol.Name + "Async")
														 .SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
						return MethodSymbolAnalyzeResult.Invalid;
					}
					asyncMethods.Add(asyncConterPart);
				}
				if (!CanProcessSyntaxReference(syntax))
				{
					continue;
				}
				interfaceMethods.Add(interfaceMember.OriginalDefinition);
			}

			// Verify if there is already an async counterpart for this method
			IMethodSymbol asyncCounterpart = null;
			if (_configuration.FindAsyncCounterpartDelegates.Any())
			{
				foreach (var findAsyncConterpart in _configuration.FindAsyncCounterpartDelegates)
				{
					asyncCounterpart = await findAsyncConterpart(ProjectData.Project, methodSymbol.OriginalDefinition, true).ConfigureAwait(false);
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
				_cachedMethodSymbolInfos.AddOrUpdate(methodSymbol.OriginalDefinition, MethodSymbolAnalyzeResult.Invalid, (symbol, info) => MethodSymbolAnalyzeResult.Invalid);
				return MethodSymbolAnalyzeResult.Invalid;
			}

			var isSymbolValidFunc = _configuration.MethodSelectionPredicate;
			if (isSymbolValidFunc != null && !isSymbolValidFunc(methodSymbol))
			{
				Logger.Debug($"Method {methodSymbol} will be ignored because of MethodSelectionPredicate");
				_cachedMethodSymbolInfos.AddOrUpdate(methodSymbol.OriginalDefinition, MethodSymbolAnalyzeResult.Invalid, (symbol, info) => MethodSymbolAnalyzeResult.Invalid);
				return MethodSymbolAnalyzeResult.Invalid;
			}

			if (methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
			{
				interfaceMethods.Add(methodSymbol);
			}
			methodSymbol = methodSymbol.OriginalDefinition; // unwrap method

			result = new MethodSymbolAnalyzeResult
			{
				IsValid = true,
				MethodSymbol = methodSymbol,
				AsyncInterfaceMethods = asyncMethods,
				InterfaceMethods = interfaceMethods,
				BaseOverriddenMethod = overridenMethod?.DeclaringSyntaxReferences.Length > 0
					? overridenMethod.OriginalDefinition
					: null,
				OverriddenMethods = overrrides
			};
			_cachedMethodSymbolInfos.AddOrUpdate(methodSymbol, result, (symbol, info) => result);
			return result;
		}

		#region ScanAllMethodReferenceLocations

		private readonly ConcurrentDictionary<IMethodSymbol, byte> _scannedMethodReferenceSymbols = new ConcurrentDictionary<IMethodSymbol, byte>();

		private async Task ScanAllMethodReferenceLocations(IMethodSymbol methodSymbol, int depth = 0)
		{
			if (_scannedMethodReferenceSymbols.ContainsKey(methodSymbol))
			{
				return;
			}
			_scannedMethodReferenceSymbols.AddOrUpdate(methodSymbol, 1, (symbol, b) => b);

			var references = await SymbolFinder.FindReferencesAsync(methodSymbol, ProjectData.Project.Solution, _analyzeDocuments).ConfigureAwait(false);

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
				// Do not check if the method was already analyzed as there can be many references inside one method
				var analyzeResult = await AnalyzeMethodSymbol(refMethodSymbol, false).ConfigureAwait(false);
				if (!analyzeResult.IsValid)
				{
					continue;
				}
				// Save the reference as it can be made async
				var methodData = documentData.GetOrCreateMethodData(analyzeResult.MethodSymbol);
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
				var invokedMethodDocInfoTask = invokedSymbol.DeclaringSyntaxReferences
																  .Select(GetOrCreateDocumentData)
																  .SingleOrDefault();
				if (invokedMethodDocInfoTask != null)
				{
					var invokedMethodDocInfo = await invokedMethodDocInfoTask;
					if (invokedMethodDocInfo != null)
					{
						var invokedMethodInfo = invokedMethodDocInfo.GetOrCreateMethodInfo(invokedSymbol);
						if (!invokedMethodInfo.InvokedBy.Contains(methodData))
						{
							invokedMethodInfo.InvokedBy.Add(methodData);
						}
					}
				}
				await ProcessMethodSymbolInfo(analyzeResult, documentData, depth).ConfigureAwait(false);
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

		private async Task ScanForTypeMissingAsyncMethods(TypeData typeData)
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
			var methodDatas = new List<MethodData>();

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
				methodDatas.Add(methodData);
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
					methodDatas.Add(methodData);
				}
				baseType = baseType.BaseType;
			}

			if (_configuration.ScanMethodBody)
			{
				var asnycCounterparts = new List<AsyncCounterpartMethod>();
				foreach (var methodData in methodDatas)
				{
					asnycCounterparts.AddRange(await FindAsyncCounterpartMethodsWhitinMethod(methodData).ConfigureAwait(false));
				}
				foreach (var group in asnycCounterparts.GroupBy(o => o.MethodSymbol))
				{
					await ScanAllMethodReferenceLocations(group.Key).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// When a type needs to be defined as a new type we need to find all references to them.
		/// Reference can point to a variable, field, base type, argument definition
		/// </summary>
		private async Task ScanForTypeReferences(TypeData typeData)
		{
			// References for ctor of the type and the type itself wont have any locations
			var references = await SymbolFinder.FindReferencesAsync(typeData.Symbol, ProjectData.Project.Solution, _analyzeDocuments).ConfigureAwait(false);
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

			// Documents that can be analyzed
			_analyzeDocuments = ProjectData.Project.Documents.Where(o => _configuration.DocumentSelectionPredicate(o)).ToImmutableHashSet();
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
