using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

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

		public ConcurrentSet<CrefFunctionReferenceData> CrefReferences { get; } = new ConcurrentSet<CrefFunctionReferenceData>();

		/// <summary>
		/// Contains references of itself
		/// </summary>
		public ConcurrentSet<TypeReferenceData> SelfReferences { get; } = new ConcurrentSet<TypeReferenceData>();

		public TypeData ParentTypeData { get; }

		public NamespaceData NamespaceData { get; }

		public INamedTypeSymbol Symbol { get; }

		public TypeDeclarationSyntax Node { get; }

		public TypeConversion Conversion { get; internal set; }

		public override ISymbol GetSymbol()
		{
			return Symbol;
		}

		internal void Copy()
		{
			Conversion = TypeConversion.Copy;
			foreach (var typeData in GetSelfAndDescendantsTypeData().Where(o => o.Conversion != TypeConversion.Ignore))
			{
				typeData.Conversion = TypeConversion.Copy;
			}
		}

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			IgnoredReason = reason;
			ExplicitlyIgnored = explicitlyIgnored;
			Conversion = TypeConversion.Ignore;
			foreach (var typeData in GetSelfAndDescendantsTypeData().Where(o => o.Conversion != TypeConversion.Ignore))
			{
				typeData.Ignore("Cascade ignored.");
			}
		}

		public bool IsPartial { get; set; }

		public ConcurrentDictionary<MethodDeclarationSyntax, MethodData> Methods { get; } = new ConcurrentDictionary<MethodDeclarationSyntax, MethodData>();

		public ConcurrentDictionary<BaseMethodDeclarationSyntax, BaseMethodData> SpecialMethods { get; } = new ConcurrentDictionary<BaseMethodDeclarationSyntax, BaseMethodData>();

		public ConcurrentDictionary<PropertyDeclarationSyntax, PropertyData> Properties { get; } = new ConcurrentDictionary<PropertyDeclarationSyntax, PropertyData>();

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> NestedTypes { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		public IEnumerable<MethodOrAccessorData> MethodsAndAccessors
		{
			get { return Methods.Values.Cast<MethodOrAccessorData>().Union(Properties.Values.SelectMany(o => o.GetAccessors())); }
		}

		public override SyntaxNode GetNode()
		{
			return Node;
		}

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

		public IEnumerable<TypeData> GetSelfAndAncestorsTypeData()
		{
			var current = this;
			while (current != null)
			{
				yield return current;
				current = current.ParentTypeData;
			}
		}

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

		public TypeData GetNestedTypeData(TypeDeclarationSyntax node, SemanticModel semanticModel, bool create = false)
		{
			if (NestedTypes.TryGetValue(node, out TypeData typeData))
			{
				return typeData;
			}
			var symbol = semanticModel.GetDeclaredSymbol(node);
			return !create ? null : NestedTypes.GetOrAdd(node, syntax => new TypeData(NamespaceData, symbol, node, this));
		}

		public MethodData GetMethodData(MethodDeclarationSyntax methodNode, SemanticModel semanticModel, bool create = false)
		{
			if (Methods.TryGetValue(methodNode, out MethodData methodData))
			{
				return methodData;
			}
			var methodSymbol = semanticModel.GetDeclaredSymbol(methodNode);
			return !create ? null : Methods.GetOrAdd(methodNode, syntax => new MethodData(this, methodSymbol, methodNode));
		}

		public BaseMethodData GetSpecialMethodData(BaseMethodDeclarationSyntax methodNode, SemanticModel semanticModel, bool create = false)
		{
			if (SpecialMethods.TryGetValue(methodNode, out BaseMethodData methodData))
			{
				return methodData;
			}
			var methodSymbol = semanticModel.GetDeclaredSymbol(methodNode);
			return !create ? null : SpecialMethods.GetOrAdd(methodNode, syntax => new BaseMethodData(this, methodSymbol, methodNode));
		}

		public PropertyData GetPropertyData(PropertyDeclarationSyntax node, SemanticModel semanticModel, bool create = false)
		{
			if (Properties.TryGetValue(node, out PropertyData data))
			{
				return data;
			}
			var symbol = semanticModel.GetDeclaredSymbol(node);
			return !create ? null : Properties.GetOrAdd(node, syntax => new PropertyData(this, symbol, node));
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

		private IReadOnlyList<IPropertyAnalyzationResult> _cachedProperties;
		IReadOnlyList<IPropertyAnalyzationResult> ITypeAnalyzationResult.Properties => _cachedProperties ?? (_cachedProperties = Properties.Values.ToImmutableArray());

		private IReadOnlyList<IFunctionAnalyzationResult> _cachedSpecialMethods;
		IReadOnlyList<IFunctionAnalyzationResult> ITypeAnalyzationResult.SpecialMethods => _cachedSpecialMethods ?? (_cachedSpecialMethods = SpecialMethods.Values.ToImmutableArray());

		#endregion

		#region IMemberAnalyzationResult

		public IMemberAnalyzationResult GetNext()
		{
			// Try to find the next sibling that can be a type or a namespace
			var sibling = NamespaceData.NestedNamespaces.Values
				.Cast<IMemberAnalyzationResult>()
				.Union(NamespaceData.Types.Values.Where(o => o != this))
				.OrderBy(o => o.GetNode().SpanStart)
				.FirstOrDefault(o => o.GetNode().SpanStart > Node.Span.End);
			return (sibling ?? ParentTypeData) ?? NamespaceData;
		}

		public IMemberAnalyzationResult GetPrevious()
		{
			// Try to find the previous sibling that can be a type or a namespace
			var sibling = NamespaceData.NestedNamespaces.Values
				.Cast<IMemberAnalyzationResult>()
				.Union(NamespaceData.Types.Values.Where(o => o != this))
				.OrderByDescending(o => o.GetNode().Span.End)
				.FirstOrDefault(o => o.GetNode().Span.End < Node.SpanStart);
			return (sibling ?? ParentTypeData) ?? NamespaceData;
		}

		public bool IsParent(IAnalyzationResult analyzationResult)
		{
			return ParentTypeData == analyzationResult || NamespaceData == analyzationResult;
		}

		#endregion
	}
}
