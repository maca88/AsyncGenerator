using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class AnonymousFunctionData : FunctionData, IAnonymousFunctionAnalyzationResult
	{
		public AnonymousFunctionData(MethodData methodData, IMethodSymbol symbol, AnonymousFunctionExpressionSyntax node,
			AnonymousFunctionData parent = null) : base(symbol)
		{
			MethodData = methodData;
			Node = node;
			ParentAnonymousFunctionData = parent;
		}

		public AnonymousFunctionExpressionSyntax Node { get; }

		public MethodData MethodData { get; }

		

		public override TypeData TypeData => MethodData.TypeData;

		/// <summary>
		/// Symbol of the method that uses this function as an argument, value represents the index of the argument
		/// </summary>
		public Tuple<IMethodSymbol, int> ArgumentOfMethod { get; set; }

		public AnonymousFunctionData ParentAnonymousFunctionData { get; }

		public ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData> NestedAnonymousFunctions { get; }
			= new ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData>();

		//public AnonymousFunctionData GetNestedAnonymousFunctionData(AnonymousFunctionExpressionSyntax node, bool create = false)
		//{
		//	var symbol = MethodData.TypeData.NamespaceData.DocumentData.SemanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
		//	return GetNestedAnonymousFunctionData(node, symbol, create);
		//}

		public AnonymousFunctionData GetNestedAnonymousFunctionData(AnonymousFunctionExpressionSyntax node,
			IMethodSymbol symbol, bool create = false)
		{
			AnonymousFunctionData typeData;
			if (NestedAnonymousFunctions.TryGetValue(node, out typeData))
			{
				return typeData;
			}
			return !create
				? null
				: NestedAnonymousFunctions.GetOrAdd(node, syntax => new AnonymousFunctionData(MethodData, symbol, node, this));
		}

		public IEnumerable<AnonymousFunctionData> GetSelfAndDescendantsAnonymousFunctionData(
			Func<AnonymousFunctionData, bool> predicate = null)
		{
			return GetSelfAndDescendantsAnonymousFunctionDataRecursively(this, predicate);
		}

		private IEnumerable<AnonymousFunctionData> GetSelfAndDescendantsAnonymousFunctionDataRecursively(
			AnonymousFunctionData functionData, Func<AnonymousFunctionData, bool> predicate = null)
		{
			if (predicate?.Invoke(functionData) == false)
			{
				yield break;
			}
			yield return functionData;
			foreach (var subTypeData in functionData.NestedAnonymousFunctions.Values)
			{
				if (predicate?.Invoke(subTypeData) == false)
				{
					yield break;
				}
				foreach (var td in GetSelfAndDescendantsAnonymousFunctionDataRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						yield break;
					}
					yield return td;
				}
			}
		}

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body;
		}

		public override IEnumerable<AnonymousFunctionData> GetAnonymousFunctionData()
		{
			return NestedAnonymousFunctions.Values;
		}

		public override MethodData GetMethodData()
		{
			return MethodData;
		}
	}
}
