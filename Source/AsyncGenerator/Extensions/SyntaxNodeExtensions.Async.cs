using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Extensions
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
						if ((node as AnonymousFunctionExpressionSyntax)?.AsyncKeyword.Kind() != SyntaxKind.AsyncKeyword)
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
			return methodNode
				.AddModifiers(Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)))
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertParenthesizedLambdaToAsync(ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
		{
			return ParenthesizedLambdaExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					parenthesizedLambda.ParameterList,
					parenthesizedLambda.ArrowToken,
					parenthesizedLambda.Body)
				.WithTriviaFrom(parenthesizedLambda)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertSimpleLambdaToAsync(SimpleLambdaExpressionSyntax simpleLambda)
		{
			return SimpleLambdaExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					simpleLambda.Parameter,
					simpleLambda.ArrowToken,
					simpleLambda.Body)
				.WithTriviaFrom(simpleLambda)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertAnonymousMethodToAsync(AnonymousMethodExpressionSyntax anonymousMethod)
		{
			return AnonymousMethodExpression(
					Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)),
					anonymousMethod.DelegateKeyword,
					anonymousMethod.ParameterList,
					anonymousMethod.Block)
				.WithTriviaFrom(anonymousMethod)
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		private static SyntaxNode ConvertLocalFunctionToAsync(LocalFunctionStatementSyntax localFunction)
		{
			return localFunction
				.AddModifiers(Token(TriviaList(), SyntaxKind.AsyncKeyword, TriviaList(Space)))
				.WithAdditionalAnnotations(Formatter.Annotation);
		}
	}
}
