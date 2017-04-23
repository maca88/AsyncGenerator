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
		public AnonymousFunctionData(MethodData methodData, IMethodSymbol symbol, AnonymousFunctionExpressionSyntax node,
			FunctionData parent = null) : base(symbol, parent ?? methodData)
		{
			MethodData = methodData;
			Node = node;
		}

		public AnonymousFunctionExpressionSyntax Node { get; }

		public MethodData MethodData { get; }

		public override TypeData TypeData => MethodData.TypeData;

		/// <summary>
		/// Symbol of the method that uses this function as an argument, value represents the index of the argument
		/// </summary>
		public Tuple<IMethodSymbol, int> ArgumentOfMethod { get; set; }

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body;
		}

		public override MethodData GetMethodData()
		{
			return MethodData;
		}
	}
}
