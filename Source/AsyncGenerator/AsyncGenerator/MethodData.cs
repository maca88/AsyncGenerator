using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class MethodData : FunctionData, IMethodAnalyzationResult
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node) : base(symbol)
		{
			TypeData = typeData;
			Node = node;
			InterfaceMethod = Symbol.ContainingType.TypeKind == TypeKind.Interface;
		}

		public bool InterfaceMethod { get; }

		/// <summary>
		/// Methods 
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalAsyncMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Interface members within project that the method implements
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ImplementedInterfaces { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Implementation/derived/base/interface methods
		/// </summary>
		public ConcurrentSet<MethodData> RelatedMethods { get; } = new ConcurrentSet<MethodData>();

		/// <summary>
		/// External Base/derivered or interface/implementation methods
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalRelatedMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Methods within project that the method overrides
		/// </summary>
		public ConcurrentSet<IMethodSymbol> OverridenMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Method datas that invokes this method
		/// </summary>
		public ConcurrentSet<FunctionData> InvokedBy { get; } = new ConcurrentSet<FunctionData>();

		/// <summary>
		/// Related and invoked by methods
		/// </summary>
		public IEnumerable<FunctionData> Dependencies
		{
			get { return InvokedBy.Union(RelatedMethods); }
		}

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		public IMethodSymbol BaseOverriddenMethod { get; set; }

		/// <summary>
		/// Reference to the async counterpart
		/// </summary>
		public IMethodSymbol AsyncCounterpartSymbol { get; set; }

		/// <summary>
		/// Reference to the async counterpart that has a <see cref="CancellationToken"/>
		/// </summary>
		public IMethodSymbol AsyncCounterpartWithTokenSymbol { get; set; }

		public bool CancellationTokenRequired { get; set; }

		//public MethodConversion CalculatedConversion { get; internal set; }

		public override TypeData TypeData { get; }

		public MethodDeclarationSyntax Node { get; }

		public ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData> AnonymousFunctionData { get; } = 
			new ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData>();

		#region IMethodAnalyzationResult

		private IReadOnlyList<IFunctionAnalyzationResult> _cachedInvokedBy;
		IReadOnlyList<IFunctionAnalyzationResult> IMethodAnalyzationResult.InvokedBy => _cachedInvokedBy ?? (_cachedInvokedBy = InvokedBy.ToImmutableArray());

		private IReadOnlyList<IAnonymousFunctionAnalyzationResult> _cachedAnonymousFunctions;
		IReadOnlyList<IAnonymousFunctionAnalyzationResult> IMethodAnalyzationResult.AnonymousFunctions =>
			_cachedAnonymousFunctions ?? (_cachedAnonymousFunctions = AnonymousFunctionData.Values.ToImmutableArray());

		#endregion

		public IEnumerable<AnonymousFunctionData> GetAllAnonymousFunctionData(Func<AnonymousFunctionData, bool> predicate = null)
		{
			return AnonymousFunctionData.Values
				.SelectMany(o => o.GetSelfAndDescendantsAnonymousFunctionData(predicate));
		}

		#region Scanning step

		internal bool Scanned { get; set; }

		#endregion

		#region Analyzation step

		public bool MustRunSynchronized { get; set; }

		#endregion

		public IEnumerable<MethodData> GetAllRelatedMethods()
		{
			var result = new HashSet<MethodData>();
			var deps = new HashSet<MethodData>();
			var depsQueue = new Queue<MethodData>(RelatedMethods);
			while (depsQueue.Count > 0)
			{
				var dependency = depsQueue.Dequeue();
				if (deps.Contains(dependency))
				{
					continue;
				}
				deps.Add(dependency);
				foreach (var subDependency in dependency.RelatedMethods)
				{
					if (!deps.Contains(subDependency))
					{
						depsQueue.Enqueue(subDependency);
					}
				}
				//yield return dependency;
				result.Add(dependency);
			}
			return result;
		}


		//public AnonymousFunctionData GetAnonymousFunctionData(AnonymousFunctionExpressionSyntax node, bool create = false)
		//{
		//	var symbol = TypeData.NamespaceData.DocumentData.SemanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
		//	return GetAnonymousFunctionData(node, symbol, create);
		//}

		public AnonymousFunctionData GetAnonymousFunctionData(AnonymousFunctionExpressionSyntax node, IMethodSymbol symbol, bool create = false)
		{
			AnonymousFunctionData functionData;
			if (AnonymousFunctionData.TryGetValue(node, out functionData))
			{
				return functionData;
			}
			return !create ? null : AnonymousFunctionData.GetOrAdd(node, syntax => new AnonymousFunctionData(this, symbol, node));
		}

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body ?? (SyntaxNode)Node.ExpressionBody;
		}

		public override IEnumerable<AnonymousFunctionData> GetAnonymousFunctionData()
		{
			return AnonymousFunctionData.Values;
		}

		public override MethodData GetMethodData() => this;

		//public AnonymousFunctionData GetAnonymousFunctionData(AnonymousFunctionExpressionSyntax type, bool create = false)
		//{
		//	var nestedNodes = new Stack<AnonymousFunctionExpressionSyntax>();
		//	foreach (var node in type.AncestorsAndSelf()
		//		.TakeWhile(o => !o.IsKind(SyntaxKind.MethodDeclaration))
		//		.OfType<AnonymousFunctionExpressionSyntax>())
		//	{
		//		nestedNodes.Push(node);
		//	}
		//	AnonymousFunctionData currentFunData = null;
		//	while (nestedNodes.Count > 0)
		//	{
		//		var node = nestedNodes.Pop();
		//		var typeDataDict = currentFunData?.NestedAnonymousFunctionData ?? AnonymousFunctionData;
		//		AnonymousFunctionData typeData;
		//		if (typeDataDict.TryGetValue(node, out typeData))
		//		{
		//			currentFunData = typeData;
		//			continue;
		//		}
		//		if (!create)
		//		{
		//			return null;
		//		}
		//		var funSymbol = TypeData.NamespaceData.DocumentData.SemanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
		//		currentFunData = typeDataDict.GetOrAdd(node, k => new AnonymousFunctionData(this, node, funSymbol, currentFunData));
		//	}
		//	return currentFunData;
		//}

	}
}
