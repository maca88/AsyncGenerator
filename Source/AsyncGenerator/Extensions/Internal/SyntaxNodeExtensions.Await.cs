using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace AsyncGenerator.Extensions.Internal
{
	static partial class SyntaxNodeExtensions
	{
		internal static ExpressionSyntax AddAwait(this ExpressionSyntax expression, ExpressionSyntax configureAwaitArgument)
		{
			var awaitNode = ConvertToAwaitExpression(expression);
			if (configureAwaitArgument != null)
			{
				awaitNode = SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, awaitNode, SyntaxFactory.IdentifierName("ConfigureAwait")))
					.WithArgumentList(
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.Argument(configureAwaitArgument))
						));
			}

			var nextToken = (expression.Parent).ChildNodesAndTokens().FirstOrDefault(o => o.SpanStart >= expression.Span.End); // token can be in a new line
			if (nextToken.IsKind(SyntaxKind.DotToken) || nextToken.IsKind(SyntaxKind.BracketedArgumentList))
			{
				awaitNode = SyntaxFactory.ParenthesizedExpression(awaitNode);
			}

			return awaitNode;
		}

		private static ExpressionSyntax Parenthesize(this ExpressionSyntax expression, bool includeElasticTrivia = true)
		{
			if (includeElasticTrivia)
			{
				return SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia())
					.WithTriviaFrom(expression)
					.WithAdditionalAnnotations(Simplifier.Annotation);
			}
			return SyntaxFactory.ParenthesizedExpression
				(
					SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
					expression.WithoutTrivia(),
					SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty)
				)
				.WithTriviaFrom(expression)
				.WithAdditionalAnnotations(Simplifier.Annotation);
		}

		private static ExpressionSyntax ConvertToAwaitExpression(ExpressionSyntax expression)
		{
			if ((expression is BinaryExpressionSyntax || expression is ConditionalExpressionSyntax) && expression.HasTrailingTrivia)
			{
				var expWithTrailing = expression.WithoutLeadingTrivia();
				var span = expWithTrailing.GetLocation().GetLineSpan().Span;
				if (span.Start.Line == span.End.Line && !expWithTrailing.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)))
				{
					return SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.AwaitKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)), SyntaxFactory.ParenthesizedExpression(expWithTrailing))
						.WithLeadingTrivia(expression.GetLeadingTrivia())
						.WithAdditionalAnnotations(Formatter.Annotation);
				}
			}

			return SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.AwaitKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)), expression.WithoutTrivia().Parenthesize())
				.WithTriviaFrom(expression)
				.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
		}
	}
}
