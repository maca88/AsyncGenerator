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
		public LocalFunctionData(BaseMethodData methodData, IMethodSymbol symbol, LocalFunctionStatementSyntax node,
			FunctionData parent = null) : base(symbol, parent ?? methodData)
		{
			MethodData = methodData ?? throw new ArgumentNullException(nameof(methodData));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public LocalFunctionStatementSyntax Node { get; }

		public BaseMethodData MethodData { get; }

		public override TypeData TypeData => MethodData.TypeData;

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return Node.Body ?? (SyntaxNode)Node.ExpressionBody;
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
