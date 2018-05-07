using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class BaseMethodData : FunctionData
	{
		private readonly SyntaxNode _node;
		private readonly SyntaxNode _bodyNode;

		public BaseMethodData(TypeData typeData, IMethodSymbol symbol, SyntaxNode node) : base(symbol)
		{
			TypeData = typeData;
			_node = node;
			// Find and set body node
			if (_node is BaseMethodDeclarationSyntax baseMethodNode)
			{
				_bodyNode = baseMethodNode.Body ?? (SyntaxNode)baseMethodNode.ExpressionBody;
			}
			else if (_node is AccessorDeclarationSyntax accessorNode) // Property getter/setter
			{
				_bodyNode = accessorNode.Body ?? (SyntaxNode)accessorNode.ExpressionBody;
			}
			else if (_node is ArrowExpressionClauseSyntax arrowNode) // Property arrow getter
			{
				_bodyNode = arrowNode;
			}
			else
			{
				throw new InvalidOperationException($"Invalid base method node {node}");
			}
		}

		/// <summary>
		/// Implementation/derived/base/interface methods inside the same project
		/// </summary>
		public ConcurrentSet<MethodOrAccessorData> RelatedMethods { get; } = new ConcurrentSet<MethodOrAccessorData>();

		public override TypeData TypeData { get; }

		public override SyntaxNode GetNode()
		{
			return _node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return _bodyNode;
		}

		public override MethodOrAccessorData GetMethodOrAccessorData() => null;

		public override BaseMethodData GetBaseMethodData() => this;
	}
}
