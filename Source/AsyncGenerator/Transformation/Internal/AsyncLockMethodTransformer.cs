using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace AsyncGenerator.Transformation.Internal
{
	internal class AsyncLockMethodTransformer : CSharpSyntaxRewriter, IMethodTransformer
	{
		private IProjectTransformConfiguration _configuration;

		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			_configuration = configuration.TransformConfiguration;
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodTransformationResult result, 
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!result.AnalyzationResult.MustRunSynchronized)
			{
				return MethodTransformerResult.Skip;
			}
			var methodSymbol = result.AnalyzationResult.Symbol;
			var fieldName = GetLockFieldName(methodSymbol, typeMetadata);
			var node = result.Transformed;
			var newBody = Block(SingletonList(
				UsingStatement(node.Body)
					.WithExpression(
						AwaitExpression(
							InvocationExpression(
								MemberAccessExpression(
									SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName(fieldName),
									IdentifierName(_configuration.AsyncLockMethodName)))))));
			node = node.WithBody(result.AnalyzationResult.Node
				.NormalizeMethodBody(newBody, result.IndentTrivia, result.EndOfLineTrivia));

			// Remove the Synchronized option from the MethodImpl attribute
			var methodImplAttr = node.AttributeLists.SelectMany(o => o.Attributes).First(o => o.Name.ToString() == "MethodImpl");
			var newMethodImplAttr = (AttributeSyntax)VisitAttribute(methodImplAttr);

			node = node.ReplaceNode(methodImplAttr, newMethodImplAttr);
			var field = GetAsyncLockField(fieldName, methodSymbol.IsStatic, result);
			return MethodTransformerResult.Update(node)
				.AddField(field);
		}

		// TODO: helper method
		private static string GetLockFieldName(IMethodSymbol symbol, ITypeTransformationMetadata typeTransformMetadata)
		{
			var fieldName = symbol.Name;
			if (!symbol.IsStatic)
			{
				fieldName = "_" + fieldName.Substring(0, 1).ToLowerInvariant() + fieldName.Substring(1);
			}
			var currentIdx = 2;
			var newFieldName = fieldName;
			while (typeTransformMetadata.MemberNames.Contains(newFieldName))
			{
				newFieldName = fieldName + currentIdx;
				currentIdx++;
			}
			return newFieldName;
		}

		private FieldDeclarationSyntax GetAsyncLockField(string fieldName, bool isStatic, IMethodTransformationResult result)
		{
			var list = TokenList(Token(TriviaList(result.LeadingWhitespaceTrivia), SyntaxKind.PrivateKeyword, TriviaList(Space)));
			if (isStatic)
			{
				list = list.Add(Token(TriviaList(), SyntaxKind.StaticKeyword, TriviaList(Space)));
			}
			list = list.Add(Token(TriviaList(), SyntaxKind.ReadOnlyKeyword, TriviaList(Space)));
			var lockType = Extensions.SyntaxNodeExtensions.ConstructNameSyntax(_configuration.AsyncLockFullTypeName, Space);
			return FieldDeclaration(
					VariableDeclaration(lockType)
						.WithVariables(
							SingletonSeparatedList(
								VariableDeclarator(
										Identifier(TriviaList(), fieldName, TriviaList(Space))
									)
									.WithInitializer(
										EqualsValueClause(
											ObjectCreationExpression(Extensions.SyntaxNodeExtensions.ConstructNameSyntax(_configuration.AsyncLockFullTypeName))
												.WithArgumentList(ArgumentList())
												.WithNewKeyword(Token(TriviaList(), SyntaxKind.NewKeyword, TriviaList(Space)))
										)
											.WithEqualsToken(Token(TriviaList(), SyntaxKind.EqualsToken, TriviaList(Space)))
									)
							)
						)
				)
				.WithModifiers(list)
				.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(result.EndOfLineTrivia)));
		}

		public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node)
		{
			var argExpression = node.Expression;
			if (argExpression is MemberAccessExpressionSyntax memberAccess)
			{
				return memberAccess.Name.ToString() == "Synchronized" ? default(SyntaxNode) : node;
			}
			if (argExpression is BinaryExpressionSyntax binary)
			{
				return node.WithExpression((ExpressionSyntax) VisitBinaryExpression(binary));
			}
			return base.VisitAttributeArgument(node);
		}

		public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
		{
			// The right expression is always a member expression
			if (node.Right is MemberAccessExpressionSyntax rightMemberAccess)
			{
				if (rightMemberAccess.Name.ToString() == "Synchronized")
				{
					if (node.Left is BinaryExpressionSyntax)
					{
						return node.Left;
					}
					return node.Left.WithoutTrailingTrivia();
				}
			}
			if (node.Left is MemberAccessExpressionSyntax leftMemberAccess)
			{
				if (leftMemberAccess.Name.ToString() == "Synchronized")
				{
					return node.Right;
				}
			}
			return base.VisitBinaryExpression(node);
		}

	}
}
