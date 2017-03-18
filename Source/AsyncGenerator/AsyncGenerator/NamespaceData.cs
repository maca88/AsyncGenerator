using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class NamespaceData : INamespaceAnalyzationResult
	{
		public NamespaceData(DocumentData documentData, INamespaceSymbol symbol, NamespaceDeclarationSyntax node)
		{
			DocumentData = documentData;
			Symbol = symbol;
			Node = node;
		}

		/// <summary>
		/// References of types that are used inside this namespace (alias to a type with a using statement)
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public DocumentData DocumentData { get; }

		public INamespaceSymbol Symbol { get; }

		public NamespaceDeclarationSyntax Node { get; }

		public bool IsGlobal => Node == null;

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> TypeData { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		//public Task<TypeData> GetTypeData(TypeDeclarationSyntax node, bool create = false)
		//{
		//	var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
		//	return GetTypeData(typeSymbol, create);
		//}

		public TypeData GetTypeData(MethodDeclarationSyntax node, bool create = false)
		{
			//var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node).ContainingType;
			var typeNode = node.Ancestors(false).OfType<TypeDeclarationSyntax>().First();
			return GetTypeData(typeNode, create);
		}

		public async Task<TypeData> GetTypeData(IMethodSymbol symbol, bool create = false)
		{
			var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == Node.SyntaxTree.FilePath);
			var memberNode = (await syntax.GetSyntaxAsync().ConfigureAwait(false)).Ancestors().OfType<TypeDeclarationSyntax>().First();
			return GetTypeData(memberNode, create);
		}

		//public async Task<TypeData> GetTypeData(INamedTypeSymbol type, bool create = false)
		//{
		//	var nestedTypes = new Stack<INamedTypeSymbol>();
		//	while (type != null)
		//	{
		//		nestedTypes.Push(type);
		//		type = type.ContainingType;
		//	}
		//	TypeData currentTypeData = null;
		//	var path = DocumentData.FilePath;
		//	while (nestedTypes.Count > 0)
		//	{
		//		var typeSymbol = nestedTypes.Pop().OriginalDefinition;
		//		//var location = typeSymbol.Locations.Single(o => o.SourceTree.FilePath == path);
		//		var syntax = typeSymbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == path);
		//		var node = (TypeDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);

		//		//var namespaceNode = Node ?? (SyntaxNode)DocumentData.RootNode; // Global namespace
		//		//var node = namespaceNode.DescendantNodes()
		//		//			   .OfType<TypeDeclarationSyntax>()
		//		//			   .First(o => o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == location.SourceSpan);

		//		var typeDataDict = currentTypeData?.NestedTypeData ?? TypeData;
		//		TypeData typeData;
		//		if (typeDataDict.TryGetValue(node, out typeData))
		//		{
		//			currentTypeData = typeData;
		//			continue;
		//		}
		//		if (!create)
		//		{
		//			return null;
		//		}
		//		currentTypeData = typeDataDict.GetOrAdd(node, k => new TypeData(this, typeSymbol, node, currentTypeData));
		//	}
		//	return currentTypeData;
		//}

		public TypeData GetTypeData(TypeDeclarationSyntax type, bool create = false)
		{
			var nestedNodes = new Stack<TypeDeclarationSyntax>();
			foreach (var node in type.AncestorsAndSelf(false).OfType<TypeDeclarationSyntax>())
			{
				nestedNodes.Push(node);
			}
			TypeData currentTypeData = null;
			while (nestedNodes.Count > 0)
			{
				var node = nestedNodes.Pop();
				var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
				//var namespaceNode = Node ?? (SyntaxNode)DocumentData.RootNode; // Global namespace
				//var node = namespaceNode.DescendantNodes()
				//			   .OfType<TypeDeclarationSyntax>()
				//			   .First(o => o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == location.SourceSpan);

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
				currentTypeData = typeDataDict.GetOrAdd(node, k => new TypeData(this, typeSymbol, node, currentTypeData));
			}
			return currentTypeData;
		}

		#region INamespaceAnalyzationResult

		IEnumerable<ReferenceLocation> INamespaceAnalyzationResult.TypeReferences => TypeReferences.ToImmutableArray();

		IEnumerable<ITypeAnalyzationResult> INamespaceAnalyzationResult.Types => TypeData.Values.ToImmutableArray();

		#endregion
	}
}
