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
	public class TypeData : ITypeAnalyzationResult
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
		/// Contains references of itself
		/// </summary>
		public ConcurrentSet<ReferenceLocation> SelfReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public TypeData ParentTypeData { get; }

		public NamespaceData NamespaceData { get; }

		public INamedTypeSymbol Symbol { get; }

		public TypeDeclarationSyntax Node { get; }

		public TypeConversion Conversion { get; internal set; }

		public ConcurrentDictionary<MethodDeclarationSyntax, MethodData> MethodData { get; } = new ConcurrentDictionary<MethodDeclarationSyntax, MethodData>();

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> NestedTypeData { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		//public IEnumerable<TypeData> GetDescendantTypeInfosAndSelf()
		//{
		//	foreach (var typeInfo in NestedTypeData.Values)
		//	{
		//		foreach (var subTypeInfo in typeInfo.GetDescendantTypeInfosAndSelf())
		//		{
		//			yield return subTypeInfo;
		//		}
		//	}
		//	yield return this;
		//}

		public IEnumerable<TypeData> GetSelfAndDescendantsTypeData(Func<TypeData, bool> predicate = null)
		{
			return GetSelfAndDescendantsTypeDataRecursively(this, predicate);
		}

		private IEnumerable<TypeData> GetSelfAndDescendantsTypeDataRecursively(TypeData typeData, Func<TypeData, bool> predicate = null)
		{
			if (predicate?.Invoke(typeData) == false)
			{
				yield break;
			}
			yield return typeData;
			foreach (var subTypeData in typeData.NestedTypeData.Values)
			{
				if (predicate?.Invoke(subTypeData) == false)
				{
					yield break;
				}
				foreach (var td in GetSelfAndDescendantsTypeDataRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						yield break;
					}
					yield return td;
				}
			}
		}

		//public TypeData GetNestedTypeData(TypeDeclarationSyntax typeNode, bool create = false)
		//{
		//	var typeSymbol = NamespaceData.DocumentData.SemanticModel.GetDeclaredSymbol(typeNode);
		//	return GetNestedTypeData(typeNode, typeSymbol, create);
		//}

		public TypeData GetNestedTypeData(TypeDeclarationSyntax node, INamedTypeSymbol symbol, bool create = false)
		{
			TypeData typeData;
			if (NestedTypeData.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			return !create ? null : NestedTypeData.GetOrAdd(node, syntax => new TypeData(NamespaceData, symbol, node, this));
		}

		//public MethodData GetMethodData(MethodDeclarationSyntax methodNode, bool create = false)
		//{
		//	var methodSymbol = NamespaceData.DocumentData.SemanticModel.GetDeclaredSymbol(methodNode);
		//	return GetMethodData(methodNode, methodSymbol, create);
		//}

		//public async Task<MethodData> GetMethodData(IMethodSymbol symbol, bool create = false)
		//{
		//	var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == Node.SyntaxTree.FilePath);
		//	var memberNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);

		//	//var location = symbol.Locations.Single(o => o.SourceTree.FilePath == Node.SyntaxTree.FilePath);
		//	//var memberNode = Node.DescendantNodes()
		//	//						 .OfType<MethodDeclarationSyntax>()
		//	//						 .First(o => o.ChildTokens().SingleOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == location.SourceSpan);
		//	return GetMethodData(memberNode, symbol, create);
		//}

		public MethodData GetMethodData(MethodDeclarationSyntax methodNode, IMethodSymbol methodSymbol, bool create = false)
		{
			MethodData methodData;
			if (MethodData.TryGetValue(methodNode, out methodData))
			{
				return methodData;
			}
			return !create ? null : MethodData.GetOrAdd(methodNode, syntax => new MethodData(this, methodSymbol, methodNode));
		}

		#region ITypeAnalyzationResult

		IEnumerable<ReferenceLocation> ITypeAnalyzationResult.TypeReferences => TypeReferences.ToImmutableArray();

		IEnumerable<ReferenceLocation> ITypeAnalyzationResult.SelfReferences => SelfReferences.ToImmutableArray();

		IEnumerable<IMethodAnalyzationResult> ITypeAnalyzationResult.Methods => MethodData.Values.ToImmutableArray();

		IEnumerable<ITypeAnalyzationResult> ITypeAnalyzationResult.NestedTypes => NestedTypeData.Values.ToImmutableArray();

		#endregion
	}
}
