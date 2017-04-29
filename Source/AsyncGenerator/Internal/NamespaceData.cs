using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class NamespaceData : AbstractData, INamespaceAnalyzationResult
	{
		public NamespaceData(DocumentData documentData, INamespaceSymbol symbol, NamespaceDeclarationSyntax node, NamespaceData parent = null)
		{
			DocumentData = documentData;
			Symbol = symbol;
			Node = node;
			ParentNamespaceData = parent;
		}

		public DocumentData DocumentData { get; }

		public INamespaceSymbol Symbol { get; }

		public NamespaceDeclarationSyntax Node { get; }

		public NamespaceConversion Conversion { get; set; }

		public NamespaceData ParentNamespaceData { get; }

		public bool IsGlobal => Node == null;

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> Types { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> NestedNamespaces { get; } = 
			new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public IEnumerable<NamespaceData> GetSelfAndDescendantsNamespaceData(Func<NamespaceData, bool> predicate = null)
		{
			return GetSelfAndDescendantsNamespaceDataRecursively(this, predicate);
		}

		private IEnumerable<NamespaceData> GetSelfAndDescendantsNamespaceDataRecursively(NamespaceData namespaceData, Func<NamespaceData, bool> predicate = null)
		{
			if (predicate?.Invoke(namespaceData) == false)
			{
				yield break;
			}
			yield return namespaceData;
			foreach (var subTypeData in namespaceData.NestedNamespaces.Values)
			{
				if (predicate?.Invoke(subTypeData) == false)
				{
					yield break;
				}
				foreach (var td in GetSelfAndDescendantsNamespaceDataRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						yield break;
					}
					yield return td;
				}
			}
		}


		//public Task<TypeData> GetTypeData(TypeDeclarationSyntax node, bool create = false)
		//{
		//	var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
		//	return GetTypeData(typeSymbol, create);
		//}

		//public TypeData GetTypeData(SyntaxNode node, bool create = false)
		//{
		//	//var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node).ContainingType;
		//	var typeNode = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
		//	return GetTypeData(typeNode, create);
		//}

		//public async Task<TypeData> GetTypeData(IMethodSymbol symbol, bool create = false)
		//{
		//	var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == Node.SyntaxTree.FilePath);
		//	var memberNode = (await syntax.GetSyntaxAsync().ConfigureAwait(false)).Ancestors().OfType<TypeDeclarationSyntax>().First();
		//	return GetTypeData(memberNode, create);
		//}

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

		public TypeData GetTypeData(TypeDeclarationSyntax typeNode, bool create = false)
		{
			var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(typeNode);
			return GetTypeData(typeNode, typeSymbol, create);
		}

		public TypeData GetTypeData(TypeDeclarationSyntax node, INamedTypeSymbol symbol, bool create = false)
		{
			TypeData typeData;
			if (Types.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			return !create ? null : Types.GetOrAdd(node, syntax => new TypeData(this, symbol, node));
		}

		public NamespaceData GetNestedNamespaceData(NamespaceDeclarationSyntax node, bool create = false)
		{
			var symbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
			return GetNestedNamespaceData(node, symbol, create);
		}

		public NamespaceData GetNestedNamespaceData(NamespaceDeclarationSyntax node, INamespaceSymbol symbol, bool create = false)
		{
			NamespaceData typeData;
			if (NestedNamespaces.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			return !create ? null : NestedNamespaces.GetOrAdd(node, syntax => new NamespaceData(DocumentData, symbol, node, this));
		}

		//public TypeData GetTypeData(TypeDeclarationSyntax type, bool create = false)
		//{
		//	var nestedNodes = new Stack<TypeDeclarationSyntax>();
		//	foreach (var node in type.AncestorsAndSelf()
		//		.TakeWhile(o => !o.IsKind(SyntaxKind.NamespaceDeclaration))
		//		.OfType<TypeDeclarationSyntax>())
		//	{
		//		nestedNodes.Push(node);
		//	}
		//	TypeData currentTypeData = null;
		//	while (nestedNodes.Count > 0)
		//	{
		//		var node = nestedNodes.Pop();
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
		//		var typeSymbol = DocumentData.SemanticModel.GetDeclaredSymbol(node);
		//		currentTypeData = typeDataDict.GetOrAdd(node, k => new TypeData(this, typeSymbol, node, currentTypeData));
		//	}
		//	return currentTypeData;
		//}

		#region INamespaceAnalyzationResult

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> INamespaceAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = TypeReferences.ToImmutableArray());

		private IReadOnlyList<ITypeAnalyzationResult> _cachedTypes;
		IReadOnlyList<ITypeAnalyzationResult> INamespaceAnalyzationResult.Types => _cachedTypes ?? (_cachedTypes = Types.Values.ToImmutableArray());

		private IReadOnlyList<INamespaceAnalyzationResult> _nestedNamespaces;
		IReadOnlyList<INamespaceAnalyzationResult> INamespaceAnalyzationResult.NestedNamespaces => _nestedNamespaces ?? (_nestedNamespaces = NestedNamespaces.Values.ToImmutableArray());

		#endregion

		#region IMemberAnalyzationResult

		public IMemberAnalyzationResult GetNext()
		{
			if (ParentNamespaceData == null)
			{
				return DocumentData.GlobalNamespaceData.Types.Values
					.OrderBy(o => o.Node.SpanStart)
					.FirstOrDefault(o => o.Node.SpanStart > Node.Span.End);
			}
			// Try to find the next sibling that can be a type or a namespace
			var sibling = ParentNamespaceData.NestedNamespaces.Values
				.Cast<IMemberAnalyzationResult>()
				.Where(o => o != this)
				.Union(ParentNamespaceData.Types.Values)
				.OrderBy(o => o.GetNode().SpanStart)
				.FirstOrDefault(o => o.GetNode().SpanStart > Node.Span.End);
			return sibling ?? ParentNamespaceData;
		}

		public IMemberAnalyzationResult GetPrevious()
		{
			if (ParentNamespaceData == null)
			{
				return DocumentData.GlobalNamespaceData.Types.Values
					.OrderByDescending(o => o.Node.SpanStart)
					.FirstOrDefault(o => o.Node.Span.End < Node.SpanStart);
			}
			// Try to find the previous sibling that can be a type or a namespace
			var sibling = ParentNamespaceData.NestedNamespaces.Values
				.Cast<IMemberAnalyzationResult>()
				.Where(o => o != this)
				.Union(ParentNamespaceData.Types.Values)
				.OrderByDescending(o => o.GetNode().Span.End)
				.FirstOrDefault(o => o.GetNode().Span.End < Node.SpanStart);
			return sibling ?? ParentNamespaceData;
		}

		public bool IsParent(IAnalyzationResult analyzationResult)
		{
			return ParentNamespaceData == analyzationResult || DocumentData == analyzationResult;
		}

		#endregion
	}
}
