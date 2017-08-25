using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class ProjectData : IProjectAnalyzationResult
	{
		private readonly SolutionData _solutionData;
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _typeNamespace =
			new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();
		private Project _project;

		public ProjectData(SolutionData solutionData, ProjectId projectId, ProjectConfiguration configuration)
		{
			_solutionData = solutionData;
			Configuration = configuration;
			ProjectId = projectId;
			DirectoryPath = Path.GetDirectoryName(Project.FilePath) + @"\";
		}

		public ProjectData(Project project, ProjectConfiguration configuration)
		{
			_project = project;
			Configuration = configuration;
			ProjectId = project.Id;
			DirectoryPath = Path.GetDirectoryName(Project.FilePath) + @"\";
		}

		internal readonly ProjectConfiguration Configuration;

		public string DirectoryPath { get; }

		public ProjectId ProjectId { get; }

		public Project Project
		{
			get { return _project ?? _solutionData.Solution.GetProject(ProjectId); }
			set
			{
				if (_project != null)
				{
					_project = value;
				}
				else
				{
					_solutionData.Solution = value.Solution;
				}
			}
		}

		public ConcurrentDictionary<string, DocumentData> Documents { get; } = new ConcurrentDictionary<string, DocumentData>();

		public bool ContainsType(INamespaceSymbol namespaceSymbol, string typeName)
		{
			var result = false;
			while (namespaceSymbol != null && !result)
			{
				var namespaceName = namespaceSymbol.ToString();
				var dict = _typeNamespace.GetOrAdd(namespaceName, k => new ConcurrentDictionary<string, bool>());
				result |= dict.GetOrAdd(typeName, k => namespaceSymbol.ConstituentNamespaces.Any(o => o.GetMembers(typeName).Any()));
				namespaceSymbol = namespaceSymbol.ContainingNamespace;
			}
			return result;
		}

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
		/*
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
		}*/

		public MethodOrAccessorData GetMethodOrAccessorData(IMethodSymbol symbol)
		{
			return (MethodOrAccessorData)GetFunctionData(symbol);
		}

		public FunctionData GetFunctionData(IMethodSymbol methodSymbol)
		{
			var syntaxReference = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntaxReference == null)
			{
				return null;
			}
			if (!Documents.TryGetValue(syntaxReference.SyntaxTree.FilePath, out var documentData))
			{
				return null;
			}
			return documentData.GetFunctionData(methodSymbol);
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
