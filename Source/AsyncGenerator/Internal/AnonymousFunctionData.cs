using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class AnonymousFunctionData : ChildFunctionData
	{
		public AnonymousFunctionData(BaseMethodData methodData, IMethodSymbol symbol, AnonymousFunctionExpressionSyntax node,
			FunctionData parent = null) : base(symbol, parent ?? methodData)
		{
			MethodData = methodData ?? throw new ArgumentNullException(nameof(methodData));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public AnonymousFunctionExpressionSyntax Node { get; }

		public BaseMethodData MethodData { get; }

		public override TypeData TypeData => MethodData.TypeData;

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body;
		}

		public override MethodOrAccessorData GetMethodOrAccessorData()
		{
			return MethodData as MethodOrAccessorData;
		}

		public override BaseMethodData GetBaseMethodData()
		{
			return MethodData;
		}
	}
}
