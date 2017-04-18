using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class TypeData : AbstractData, ITypeAnalyzationResult
	{
		public TypeData(NamespaceData namespaceData, INamedTypeSymbol symbol, TypeDeclarationSyntax node, TypeData parentTypeData = null)
		{
			NamespaceData = namespaceData;
			ParentTypeData = parentTypeData;
			Symbol = symbol;
			Node = node;
		}

		public ConcurrentSet<CrefReferenceData> CrefReferences { get; } = new ConcurrentSet<CrefReferenceData>();

		/// <summary>
		/// Contains references of itself
		/// </summary>
		public ConcurrentSet<TypeReferenceData> SelfReferences { get; } = new ConcurrentSet<TypeReferenceData>();

		public TypeData ParentTypeData { get; }

		public NamespaceData NamespaceData { get; }

		public INamedTypeSymbol Symbol { get; }

		public TypeDeclarationSyntax Node { get; }

		public TypeConversion Conversion { get; internal set; }

		public bool IsPartial { get; set; }

		public ConcurrentDictionary<MethodDeclarationSyntax, MethodData> Methods { get; } = new ConcurrentDictionary<MethodDeclarationSyntax, MethodData>();

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> NestedTypes { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

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
			foreach (var subTypeData in typeData.NestedTypes.Values)
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

		public TypeData GetNestedTypeData(TypeDeclarationSyntax node, INamedTypeSymbol symbol, bool create = false)
		{
			TypeData typeData;
			if (NestedTypes.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			return !create ? null : NestedTypes.GetOrAdd(node, syntax => new TypeData(NamespaceData, symbol, node, this));
		}

		public MethodData GetMethodData(MethodDeclarationSyntax methodNode, IMethodSymbol methodSymbol, bool create = false)
		{
			MethodData methodData;
			if (Methods.TryGetValue(methodNode, out methodData))
			{
				return methodData;
			}
			return !create ? null : Methods.GetOrAdd(methodNode, syntax => new MethodData(this, methodSymbol, methodNode));
		}

		#region ITypeAnalyzationResult

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> ITypeAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = TypeReferences.ToImmutableArray());

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedSelfReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> ITypeAnalyzationResult.SelfReferences => _cachedSelfReferences ?? (_cachedSelfReferences = SelfReferences.ToImmutableArray());

		private IReadOnlyList<IMethodAnalyzationResult> _cachedMethods;
		IReadOnlyList<IMethodAnalyzationResult> ITypeAnalyzationResult.Methods => _cachedMethods ?? (_cachedMethods = Methods.Values.ToImmutableArray());

		private IReadOnlyList<ITypeAnalyzationResult> _cachedNestedTypes;
		IReadOnlyList<ITypeAnalyzationResult> ITypeAnalyzationResult.NestedTypes => _cachedNestedTypes ?? (_cachedNestedTypes = NestedTypes.Values.ToImmutableArray());

		#endregion
	}
}
