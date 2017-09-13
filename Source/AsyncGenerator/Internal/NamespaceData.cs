using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
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

		public override ISymbol GetSymbol()
		{
			return Symbol;
		}

		public ConcurrentDictionary<TypeDeclarationSyntax, TypeData> Types { get; } = new ConcurrentDictionary<TypeDeclarationSyntax, TypeData>();

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> NestedNamespaces { get; } = 
			new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		public bool ContainsType(string name)
		{
			return DocumentData.ProjectData.ContainsType(Symbol, name);
		}

		public bool IsIncluded(string fullNamespace)
		{
			if (fullNamespace == null)
			{
				return true; // Global namespace
			}
			if (Symbol.ToString().StartsWith(fullNamespace))
			{
				return true;
			}
			if (Node.HasUsing(fullNamespace))
			{
				return true;
			}
			if(fullNamespace == "System.Threading" && Types.Values
				.Any(o => o.GetSelfAndDescendantsTypeData().Any(t => t.MethodsAndAccessors.Any(m => m.CancellationTokenRequired))))
			{
				return true;
			}
			return false;
		}

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			IgnoredReason = reason;
			ExplicitlyIgnored = explicitlyIgnored;
			Conversion = NamespaceConversion.Ignore;
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
					continue; // We shall never retrun here in order to be always consistent
				}
				foreach (var td in GetSelfAndDescendantsNamespaceDataRecursively(subTypeData, predicate))
				{
					yield return td;
				}
			}
		}

		public TypeData GetTypeData(TypeDeclarationSyntax node, SemanticModel semanticModel, bool create = false)
		{
			TypeData typeData;
			if (Types.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			var symbol = semanticModel.GetDeclaredSymbol(node);
			return !create ? null : Types.GetOrAdd(node, syntax => new TypeData(this, symbol, node));
		}

		public NamespaceData GetNestedNamespaceData(NamespaceDeclarationSyntax node, SemanticModel semanticModel, bool create = false)
		{
			NamespaceData typeData;
			if (NestedNamespaces.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			var symbol = semanticModel.GetDeclaredSymbol(node);
			return !create ? null : NestedNamespaces.GetOrAdd(node, syntax => new NamespaceData(DocumentData, symbol, node, this));
		}

		#region INamespaceAnalyzationResult

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> INamespaceAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = References.OfType<TypeDataReference>().ToImmutableArray());

		private IReadOnlyList<ITypeAnalyzationResult> _cachedTypes;
		IReadOnlyList<ITypeAnalyzationResult> INamespaceAnalyzationResult.Types => _cachedTypes ?? (_cachedTypes = Types.Values.ToImmutableArray());

		private IReadOnlyList<INamespaceAnalyzationResult> _nestedNamespaces;
		IReadOnlyList<INamespaceAnalyzationResult> INamespaceAnalyzationResult.NestedNamespaces => _nestedNamespaces ?? (_nestedNamespaces = NestedNamespaces.Values.ToImmutableArray());

		#endregion
	}
}
