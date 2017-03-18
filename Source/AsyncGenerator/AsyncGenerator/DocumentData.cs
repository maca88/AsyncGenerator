using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class DocumentData : IDocumentAnalyzationResult
	{
		public DocumentData(ProjectData projectData, Document document, CompilationUnitSyntax node, SemanticModel semanticModel)
		{
			ProjectData = projectData;
			Document = document;
			Node = node;
			SemanticModel = semanticModel;
			GlobalNamespaceData = new NamespaceData(this, SemanticModel.Compilation.GlobalNamespace, null);
		}

		public Document Document { get; }

		public string FilePath => Document.FilePath;

		public ProjectData ProjectData { get; }

		public CompilationUnitSyntax Node { get; }

		public SemanticModel SemanticModel { get; }

		public NamespaceData GlobalNamespaceData { get; }

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> NamespaceData { get; } = new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		public IEnumerable<MethodData> GetAllMethodDatas()
		{
			return NamespaceData.Values
				.SelectMany(o => o.TypeData.Values
					.SelectMany(t => t.GetDescendantTypeInfosAndSelf())
					.SelectMany(t => t.MethodData.Values));
		}

		public IEnumerable<TypeData> GetAllTypeDatas()
		{
			return NamespaceData.Values
				.SelectMany(o => o.TypeData.Values
					.SelectMany(t => t.GetDescendantTypeInfosAndSelf()));
		}

		public async Task<MethodData> GetMethodData(IMethodSymbol symbol)
		{
			return await (await (await
				GetNamespaceData(symbol).ConfigureAwait(false))
				.GetTypeData(symbol).ConfigureAwait(false))
				.GetMethodData(symbol).ConfigureAwait(false);
		}

		public async Task<MethodData> GetOrCreateMethodData(IMethodSymbol symbol)
		{
			return await (await (await
				GetNamespaceData(symbol, true).ConfigureAwait(false))
				.GetTypeData(symbol, true).ConfigureAwait(false))
				.GetMethodData(symbol, true).ConfigureAwait(false);
		}

		public MethodData GetMethodData(MethodDeclarationSyntax node)
		{
			return GetNamespaceData(node).GetTypeData(node).GetMethodData(node);
		}

		public MethodData GetOrCreateMethodData(MethodDeclarationSyntax node)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true).GetMethodData(node, true);
		}

		public TypeData GetOrCreateTypeData(TypeDeclarationSyntax node)
		{
			return GetNamespaceData(node, true).GetTypeData(node, true);
		}

		public async Task<NamespaceData> GetNamespaceData(ISymbol symbol, bool create = false)
		{
			var namespaceSymbol = symbol.ContainingNamespace;
			if (namespaceSymbol.IsGlobalNamespace)
			{
				return GlobalNamespaceData;
			}
			var syntax = namespaceSymbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == FilePath);
			var node = (NamespaceDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
			//var location = namespaceSymbol.Locations.Single(o => o.SourceTree.FilePath == FilePath);
			//var node = RootNode.DescendantNodes()
			//				   .OfType<NamespaceDeclarationSyntax>()
			//				   .FirstOrDefault(
			//					   o =>
			//					   {
			//						   var identifier = o.ChildNodes().OfType<IdentifierNameSyntax>().SingleOrDefault();
			//						   if (identifier != null)
			//						   {
			//							   return identifier.Span == location.SourceSpan;
			//						   }
			//						   return o.ChildNodes().OfType<QualifiedNameSyntax>().Single().Right.Span == location.SourceSpan;
			//					   });
			//if (node == null) //TODO: location.SourceSpan.Start == 0 -> a bug perhaps ???
			//{
			//	node = RootNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single(o => o.FullSpan.End == location.SourceSpan.End);
			//}
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

		#region IDocumentAnalyzationResult

		IEnumerable<INamespaceAnalyzationResult> IDocumentAnalyzationResult.Namespaces => NamespaceData.Values.ToImmutableArray();

		INamespaceAnalyzationResult IDocumentAnalyzationResult.GlobalNamespace => GlobalNamespaceData;

		#endregion
	}
}
