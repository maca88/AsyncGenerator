using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal abstract class FunctionData : AbstractData, IFunctionAnalyzationResult
	{
		protected FunctionData(IMethodSymbol methodSymbol)
		{
			Symbol = methodSymbol;
		}

		//TODO: remove if not needed
		public bool IsAsync { get; set; }

		public IMethodSymbol Symbol { get; }

		public abstract TypeData TypeData { get; }

		public MethodConversion Conversion { get; set; }

		/// <summary>
		/// References to other methods that are referenced/invoked inside this function/method and are candidates to be async
		/// </summary>
		public ConcurrentSet<BodyFunctionReferenceData> BodyMethodReferences { get; } = new ConcurrentSet<BodyFunctionReferenceData>();

		public ConcurrentSet<CrefFunctionReferenceData> CrefMethodReferences { get; } = new ConcurrentSet<CrefFunctionReferenceData>();

		public ConcurrentDictionary<SyntaxNode, ChildFunctionData> ChildFunctions { get; } = new ConcurrentDictionary<SyntaxNode, ChildFunctionData>();

		public List<StatementSyntax> Preconditions { get; } = new List<StatementSyntax>();

		public abstract SyntaxNode GetBodyNode();

		public abstract MethodData GetMethodData();

		public ChildFunctionData GetChildFunction(SyntaxNode node, IMethodSymbol symbol, bool create = false)
		{
			if (ChildFunctions.TryGetValue(node, out var typeData))
			{
				return typeData;
			}
			if (!create)
			{
				return null;
			}
			var anonymousFunc = node as AnonymousFunctionExpressionSyntax;
			if (anonymousFunc != null)
			{
				return ChildFunctions.GetOrAdd(node, syntax => new AnonymousFunctionData(GetMethodData(), symbol, anonymousFunc, this));
			}
			var localFunc = node as LocalFunctionStatementSyntax;
			if (localFunc != null)
			{
				return ChildFunctions.GetOrAdd(node, syntax => new LocalFunctionData(GetMethodData(), symbol, localFunc, this));
			}
			throw new InvalidOperationException($"Cannot get a ChildFunctionData from syntax node {node}");
		}

		public IEnumerable<FunctionData> GetSelfAndDescendantsFunctions(Func<FunctionData, bool> predicate = null)
		{
			return GetSelfAndDescendantsFunctionsRecursively(this, predicate);
		}

		public IEnumerable<ChildFunctionData> GetDescendantsChildFunctions(Func<ChildFunctionData, bool> predicate = null)
		{
			return ChildFunctions.Values.SelectMany(o => o.GetSelfAndDescendantsFunctionsRecursively(o, predicate));
		}

		private IEnumerable<T> GetSelfAndDescendantsFunctionsRecursively<T>(T functionData, Func<T, bool> predicate = null)
			where T : FunctionData
		{
			if (predicate?.Invoke(functionData) == false)
			{
				yield break;
			}
			yield return functionData;
			foreach (var subTypeData in functionData.ChildFunctions.Values.OfType<T>())
			{
				if (predicate?.Invoke(subTypeData) == false)
				{
					yield break;
				}
				foreach (var td in GetSelfAndDescendantsFunctionsRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						yield break;
					}
					yield return td;
				}
			}
		}

		#region Analyze step

		public bool HasYields { get; set; }

		#endregion

		#region Post analyze step

		public bool SplitTail { get; set; }

		public bool OmitAsync { get; set; }

		public bool WrapInTryCatch { get; set; }

		#endregion

		#region IFunctionAnalyzationResult

		IMethodAnalyzationResult IFunctionAnalyzationResult.GetMethod() => GetMethodData();

		private IReadOnlyList<IBodyFunctionReferenceAnalyzationResult> _cachedMethodReferences;
		IReadOnlyList<IBodyFunctionReferenceAnalyzationResult> IFunctionAnalyzationResult.MethodReferences => _cachedMethodReferences ?? (_cachedMethodReferences = BodyMethodReferences.ToImmutableArray());

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> IFunctionAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = TypeReferences.ToImmutableArray());

		private IReadOnlyList<StatementSyntax> _cachedPreconditions;
		IReadOnlyList<StatementSyntax> IFunctionAnalyzationResult.Preconditions => _cachedPreconditions ?? (_cachedPreconditions = Preconditions.ToImmutableArray());

		private IReadOnlyList<IChildFunctionAnalyzationResult> _cachedChildFunctions;
		IReadOnlyList<IChildFunctionAnalyzationResult> IFunctionAnalyzationResult.ChildFunctions => _cachedChildFunctions ?? (_cachedChildFunctions = ChildFunctions.Values.ToImmutableArray());

		#endregion
	}
}
