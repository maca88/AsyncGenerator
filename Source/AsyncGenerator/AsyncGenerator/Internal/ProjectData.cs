using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class ProjectData : IProjectAnalyzationResult
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

		public ConcurrentDictionary<string, DocumentData> Documents { get; } = new ConcurrentDictionary<string, DocumentData>();

		public bool Contains(SyntaxReference syntax)
		{
			return Project.Solution.GetDocument(syntax.SyntaxTree).Project == Project;
		}

		public bool Contains(IMethodSymbol methodSymbol)
		{
			var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			return syntaxReference != null && Contains(syntaxReference);
		}

		public DocumentData GetDocumentData(SyntaxReference syntax)
		{
			return GetDocumentData(Project.Solution.GetDocument(syntax.SyntaxTree));
		}

		public async Task<MethodData> GetMethodData(IMethodSymbol methodSymbol)
		{
			var syntax = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntax == null || !Contains(syntax))
			{
				return null;
			}
			var documentData = GetDocumentData(syntax);
			var node = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
			return documentData.GetMethodData(node);
		}

		public Task<FunctionData> GetAnonymousFunctionOrMethodData(IMethodSymbol methodSymbol)
		{
			var syntax = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntax == null || !Contains(syntax))
			{
				return Task.FromResult<FunctionData>(null);
			}
			var documentData = GetDocumentData(syntax);
			return documentData.GetAnonymousFunctionOrMethodData(methodSymbol);
		}

		public DocumentData GetDocumentData(Document document)
		{
			DocumentData documentData;
			if (!Documents.TryGetValue(document.FilePath, out documentData))
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
			return Documents.AddOrUpdate(document.FilePath, documentData, (s, data) => documentData);
		}

		#region IProjectAnalyzationResult

		private IReadOnlyList<IDocumentAnalyzationResult> _cachedDocuments;
		IReadOnlyList<IDocumentAnalyzationResult> IProjectAnalyzationResult.Documents => _cachedDocuments ?? (_cachedDocuments = Documents.Values.ToImmutableArray());

		#endregion

	}
}
