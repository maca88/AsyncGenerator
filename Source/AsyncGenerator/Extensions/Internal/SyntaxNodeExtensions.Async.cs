using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Extensions.Internal
{
	static partial class SyntaxNodeExtensions
	{
		public static T AddAsync<T>(this T node) where T : SyntaxNode
		{
			var nodeToModify = GetContainingMember(node);
			return nodeToModify == null ? node : (T)ConvertToAsync(nodeToModify) ?? node;
		}
		/*
		public static T AddAsync<T>(this SyntaxNode oldNode, T root)
			where T : SyntaxNode
		{
			var nodeToModify = GetContainingMember(oldNode);
			if (nodeToModify == null)
			{
				return null;
			}

			var modifiedNode = ConvertToAsync(nodeToModify);
			if (modifiedNode == oldNode)
			{
				return root;
			}
			if (modifiedNode != null)
			{
				return root.ReplaceNode(nodeToModify, modifiedNode);
			}

			return null;
		}*/

		private static SyntaxNode GetContainingMember(SyntaxNode oldNode)
		{
			foreach (var node in oldNode.AncestorsAndSelf())
			{
				switch (node.Kind())
				{
					case SyntaxKind.ParenthesizedLambdaExpression:
					case SyntaxKind.SimpleLambdaExpression:
					case SyntaxKind.AnonymousMethodExpression:
						if ((node as AnonymousFunctionExpressionSyntax)?.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword) != true)
						{
							return node;
						}
						break;
					case SyntaxKind.MethodDeclaration:
						if ((node as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.AsyncKeyword) == false)
						{
							return node;
						}
						break;
					case SyntaxKind.LocalFunctionStatement:
						if ((node as LocalFunctionStatementSyntax)?.Modifiers.Any(SyntaxKind.AsyncKeyword) == false)
						{
							return node;
						}
						break;
					default:
						continue;
				}
			}

			return null;
		}

		private static SyntaxNode ConvertToAsync(SyntaxNode node)
		{
			return node.TypeSwitch(
				(MethodDeclarationSyntax methodNode) => ConvertMethodToAsync(methodNode),
				(ParenthesizedLambdaExpressionSyntax parenthesizedLambda) => ConvertParenthesizedLambdaToAsync(parenthesizedLambda),
				(SimpleLambdaExpressionSyntax simpleLambda) => ConvertSimpleLambdaToAsync(simpleLambda),
				(AnonymousMethodExpressionSyntax anonymousMethod) => ConvertAnonymousMethodToAsync(anonymousMethod),
				(LocalFunctionStatementSyntax localMethod) => ConvertLocalFunctionToAsync(localMethod),
				@default => null);
		}

		private static SyntaxNode ConvertMethodToAsync(MethodDeclarationSyntax methodNode)
		{
			var token = methodNode.GetFirstToken();
			var leadingTrivia = TriviaList();
			if (methodNode.ReturnType.GetFirstToken() == token)
			{
				methodNode = methodNode.ReplaceToken(token, token.WithLeadingTrivia(leadingTrivia));
				leadingTrivia = token.LeadingTrivia;
			}
			return methodNode
				.AddModifiers(Token(leadingTrivia, SyntaxKind.AsyncKeyword, TriviaList(Space)))
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertParenthesizedLambdaToAsync(ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
		{
			return ParenthesizedLambdaExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					parenthesizedLambda.ParameterList.WithoutLeadingTrivia(),
					parenthesizedLambda.ArrowToken,
					parenthesizedLambda.Body)
				.WithTriviaFrom(parenthesizedLambda)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertSimpleLambdaToAsync(SimpleLambdaExpressionSyntax simpleLambda)
		{
			return SimpleLambdaExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					simpleLambda.Parameter.WithoutLeadingTrivia(),
					simpleLambda.ArrowToken,
					simpleLambda.Body)
				.WithTriviaFrom(simpleLambda)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertAnonymousMethodToAsync(AnonymousMethodExpressionSyntax anonymousMethod)
		{
			return AnonymousMethodExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					anonymousMethod.DelegateKeyword.WithLeadingTrivia(TriviaList()),
					anonymousMethod.ParameterList,
					anonymousMethod.Block)
				.WithTriviaFrom(anonymousMethod)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertLocalFunctionToAsync(LocalFunctionStatementSyntax localFunction)
		{
			var token = localFunction.GetFirstToken();
			return localFunction
				.ReplaceToken(token, token.WithLeadingTrivia(TriviaList()))
				.AddModifiers(Token(token.LeadingTrivia, SyntaxKind.AsyncKeyword, TriviaList(Space)))
				.WithAdditionalAnnotations(Formatter.Annotation);
		}
	}
}
