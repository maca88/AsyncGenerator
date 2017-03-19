using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public abstract class BaseMethodData
	{
		/// <summary>
		/// References of types that are used inside this method
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<ReferenceLocation> MethodReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public abstract SyntaxNode GetNode();
	}


	public class MethodData : BaseMethodData, IMethodAnalyzationResult
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node)
		{
			TypeData = typeData;
			Symbol = symbol;
			Node = node;
			InterfaceMethod = Symbol.ContainingType.TypeKind == TypeKind.Interface;
		}

		public bool InterfaceMethod { get; }

		/// <summary>
		/// Methods 
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalAsyncMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Interface members within project that the method implements (internal and external)
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
		public ConcurrentSet<BaseMethodData> InvokedBy { get; } = new ConcurrentSet<BaseMethodData>();

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		public IMethodSymbol BaseOverriddenMethod { get; set; }

		/// <summary>
		/// Reference to the async counterpart for this method
		/// </summary>
		public IMethodSymbol AsyncCounterpartSymbol { get; set; }

		public bool IsAsync { get; set; }

		public MethodConversion Conversion { get; internal set; }

		public TypeData TypeData { get; }

		public IMethodSymbol Symbol { get; }

		public MethodDeclarationSyntax Node { get; }

		public ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData> AnonymousFunctionData { get; } = 
			new ConcurrentDictionary<AnonymousFunctionExpressionSyntax, AnonymousFunctionData>();

		#region IMethodAnalyzationResult

		//IEnumerable<IMethodAnalyzationResult> IMethodAnalyzationResult.InvokedBy => InvokedBy.ToImmutableArray();

		IEnumerable<ReferenceLocation> IMethodAnalyzationResult.MethodReferences => MethodReferences.ToImmutableArray();

		IEnumerable<ReferenceLocation> IMethodAnalyzationResult.TypeReferences => TypeReferences.ToImmutableArray();

		#endregion

		public IEnumerable<AnonymousFunctionData> GetAllAnonymousFunctionData(Func<AnonymousFunctionData, bool> predicate = null)
		{
			return AnonymousFunctionData.Values
				.SelectMany(o => o.GetSelfAndDescendantsAnonymousFunctionData(predicate));
		}

		// Analyze step

		public ConcurrentSet<MethodReferenceData> MethodReferenceData { get; } = new ConcurrentSet<MethodReferenceData>();

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
