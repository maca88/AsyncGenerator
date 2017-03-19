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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace AsyncGenerator
{
	public interface IMethodAnalyzationResult
	{
		MethodConversion Conversion { get; }

		IMethodSymbol Symbol { get; }

		MethodDeclarationSyntax Node { get; }

		bool IsAsync { get; }

		/// <summary>
		/// References of types that are used inside this method
		/// </summary>
		IEnumerable<ReferenceLocation> TypeReferences { get; }

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		IEnumerable<ReferenceLocation> MethodReferences { get; }

		/// <summary>
		/// Methods that invokes this method
		/// </summary>
		//IEnumerable<IMethodAnalyzationResult> InvokedBy { get; }

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		IMethodSymbol BaseOverriddenMethod { get; }

		/// <summary>
		/// Reference to the async counterpart for this method
		/// </summary>
		IMethodSymbol AsyncCounterpartSymbol { get; }
	}

	public interface ITypeAnalyzationResult
	{
		INamedTypeSymbol Symbol { get; }

		TypeDeclarationSyntax Node { get; }

		TypeConversion Conversion { get; }

		/// <summary>
		/// References of types that are used inside this type
		/// </summary>
		IEnumerable<ReferenceLocation> TypeReferences { get; }

		/// <summary>
		/// References of itself
		/// </summary>
		IEnumerable<ReferenceLocation> SelfReferences { get; }

		IEnumerable<ITypeAnalyzationResult> NestedTypes { get; }

		IEnumerable<IMethodAnalyzationResult> Methods { get; }
	}

	public interface INamespaceAnalyzationResult
	{
		NamespaceDeclarationSyntax Node { get; }

		INamespaceSymbol Symbol { get; }

		/// <summary>
		/// References of types that are used inside this namespace (alias to a type with a using statement)
		/// </summary>
		IEnumerable<ReferenceLocation> TypeReferences { get; }

		IEnumerable<ITypeAnalyzationResult> Types { get; }
	}

	public interface IDocumentAnalyzationResult
	{
		Document Document { get; }

		CompilationUnitSyntax Node { get; }

		IEnumerable<INamespaceAnalyzationResult> Namespaces { get; }

		INamespaceAnalyzationResult GlobalNamespace { get; }
	}

	public interface IProjectAnalyzationResult
	{
		Project Project { get; }

		IEnumerable<IDocumentAnalyzationResult> Documents { get; }
	}

	public class MethodReferenceData
	{
		public MethodReferenceData(MethodData methodData, ReferenceLocation reference, SimpleNameSyntax referenceNode, 
			IMethodSymbol referenceSymbol, MethodData referenceMethodData)
		{
			MethodData = methodData;
			ReferenceLocation = reference;
			ReferenceNode = referenceNode;
			ReferenceSymbol = referenceSymbol;
			ReferenceMethodData = referenceMethodData;
		}

		public MethodData MethodData { get; }

		public MethodData ReferenceMethodData { get; }

		public SimpleNameSyntax ReferenceNode { get; }

		public ReferenceLocation ReferenceLocation { get; }

		public IMethodSymbol ReferenceSymbol { get; }

		public bool CanBeAsync { get; set; }

		public bool CanBeAwaited { get; internal set; } = true;

		public bool PassedAsArgument { get; internal set; }

		public bool MakeAnonymousFunctionAsync { get; set; }

		public bool UsedAsReturnValue { get; internal set; }

		public override int GetHashCode()
		{
			return ReferenceLocation.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != GetType())
			{
				return false;
			}
			return ReferenceLocation.Equals(((MethodReferenceData)obj).ReferenceLocation);
		}
	}

	public class ProjectData : IProjectAnalyzationResult
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

		#region IProjectAnalyzationResult

		IEnumerable<IDocumentAnalyzationResult> IProjectAnalyzationResult.Documents => DocumentData.Values.ToImmutableArray();

		#endregion

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
			var props = new Dictionary<string, string>
			{
				["CheckForSystemRuntimeDependency"] = "true" // needed in order that project references are loaded
			};
			var workspace = MSBuildWorkspace.Create(props);
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
