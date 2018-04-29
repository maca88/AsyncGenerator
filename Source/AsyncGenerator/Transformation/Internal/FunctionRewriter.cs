using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// A rewriter that will traverse only the first function
	/// </summary>
	internal class FunctionRewriter : CSharpSyntaxRewriter
	{
		/// <summary>
		/// This kind of the function being rewritten
		/// </summary>
		protected SyntaxKind? RewritingSyntaxKind { get; private set; }

		#region VisitMethodDeclaration

		public sealed override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			RewritingSyntaxKind = node.Kind();
			return OnVisitMethodDeclaration(node);
		}

		protected virtual SyntaxNode OnVisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			return base.VisitMethodDeclaration(node);
		}

		#endregion

		#region VisitAnonymousMethodExpression

		public sealed override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			if (RewritingSyntaxKind.HasValue)
			{
				return node;
			}
			RewritingSyntaxKind = node.Kind();
			return OnVisitAnonymousMethodExpression(node);
		}

		protected virtual SyntaxNode OnVisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			return base.VisitAnonymousMethodExpression(node);
		}

		#endregion

		#region VisitParenthesizedLambdaExpression

		public sealed override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			if (RewritingSyntaxKind.HasValue)
			{
				return node;
			}
			RewritingSyntaxKind = node.Kind();
			return OnVisitParenthesizedLambdaExpression(node);
		}

		protected virtual SyntaxNode OnVisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			return base.VisitParenthesizedLambdaExpression(node);
		}

		#endregion

		#region VisitSimpleLambdaExpression

		public sealed override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			if (RewritingSyntaxKind.HasValue)
			{
				return node;
			}
			RewritingSyntaxKind = node.Kind();
			return OnVisitSimpleLambdaExpression(node);
		}

		protected virtual SyntaxNode OnVisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			return base.VisitSimpleLambdaExpression(node);
		}

		#endregion

		#region VisitLocalFunctionStatement

		public sealed override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
		{
			if (RewritingSyntaxKind.HasValue)
			{
				return node;
			}
			RewritingSyntaxKind = node.Kind();
			return OnVisitLocalFunctionStatement(node);
		}

		protected virtual SyntaxNode OnVisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
		{
			return base.VisitLocalFunctionStatement(node);
		}

		#endregion

	}
}
