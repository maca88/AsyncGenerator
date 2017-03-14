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
		public HashSet<ReferenceLocation> TypeReferences { get; } = new HashSet<ReferenceLocation>();

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
		public HashSet<ReferenceLocation> TypeReferences { get; } = new HashSet<ReferenceLocation>();

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
		public HashSet<ReferenceLocation> TypeReferences { get; } = new HashSet<ReferenceLocation>();

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
		/*
		public MethodData GetOrCreateMethodData(IMethodSymbol symbol)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true).GetMethodData(symbol, true);
		}*/

		public TypeData GetOrCreateTypeData(TypeDeclarationSyntax node)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true);
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
				typeData.TypeReferences.Add(refLocation);

				// we need to find the type where the reference location is
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
					var methodInfo = documentData.GetOrCreateMethodData(methodNode);
					if (methodInfo.TypeReferences.Contains(refLocation))
					{
						continue;
					}
					methodInfo.TypeReferences.Add(refLocation);
				}
				else
				{
					var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
					if (type != null)
					{
						var refTypeInfo = documentData.GetOrCreateTypeData(type);
						if (refTypeInfo.TypeReferences.Contains(refLocation))
						{
							continue;
						}
						refTypeInfo.TypeReferences.Add(refLocation);
					}
					else // can happen when declaring a Name in a using statement
					{
						var namespaceNode = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
						var namespaceInfo = documentData.GetNamespaceData(namespaceNode, true);
						if (namespaceInfo.TypeReferences.Contains(refLocation))
						{
							continue;
						}
						namespaceInfo.TypeReferences.Add(refLocation);
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
