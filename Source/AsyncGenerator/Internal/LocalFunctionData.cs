using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class LocalFunctionData : ChildFunctionData
	{
		public LocalFunctionData(MethodData methodData, IMethodSymbol symbol, LocalFunctionStatementSyntax node,
			FunctionData parent = null) : base(symbol, parent ?? methodData)
		{
			MethodData = methodData;
			Node = node;
		}

		public LocalFunctionStatementSyntax Node { get; }

		public MethodData MethodData { get; }

		public override TypeData TypeData => MethodData.TypeData;

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body ?? (SyntaxNode)Node.ExpressionBody;
		}

		public override MethodData GetMethodData()
		{
			return MethodData;
		}
	}
}
