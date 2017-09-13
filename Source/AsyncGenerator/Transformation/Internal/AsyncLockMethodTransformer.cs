using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = AsyncGenerator.Extensions.Internal.SyntaxNodeExtensions;


namespace AsyncGenerator.Transformation.Internal
{
	internal class AsyncLockMethodTransformer : CSharpSyntaxRewriter, IMethodOrAccessorTransformer
	{
		private IProjectTransformConfiguration _configuration;

		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			_configuration = configuration.TransformConfiguration;
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult result, 
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!result.TransformedLocks.Any() && !result.AnalyzationResult.MustRunSynchronized)
			{
				return MethodTransformerResult.Skip;
			}
			var newFields = new Dictionary<string, FieldDeclarationSyntax>();
			var node = result.Transformed;
			// Transform all lock statements that contains at least one async invocation.
			foreach (var lockResult in result.TransformedLocks)
			{
				var lockNode = node.GetAnnotatedNodes(lockResult.Annotation).OfType<LockStatementSyntax>().First();
				if (result.AnalyzationResult.FunctionReferences
					.Where(r => lockResult.AnalyzationResult.Node.Span.Contains(r.ReferenceNameNode.Span))
					.All(r => r.GetConversion() != ReferenceConversion.ToAsync))
				{
					continue;
				}
				var lockSymbol = lockResult.AnalyzationResult.Symbol;
				var typeLock = lockSymbol is INamedTypeSymbol;
				var isStatic = lockSymbol.IsStatic || typeLock;
				var lockFieldName = GetLockFieldName(lockResult.AnalyzationResult.Symbol, isStatic, typeLock ? "Lock" : "",  "Async");
				// TODO: handle name collisions
				if (!typeMetadata.MemberNames.Contains(lockFieldName) && !newFields.ContainsKey(lockFieldName))
				{
					newFields.Add(lockFieldName, GetAsyncLockField(lockFieldName, isStatic, result));
				}

				var usingNode = GetUsingAsyncLock(lockFieldName, lockNode.GetLeadingTrivia(),
						lockNode.CloseParenToken.TrailingTrivia, lockNode.Statement);
				node = node.ReplaceNode(lockNode, usingNode);
			}

			if (result.AnalyzationResult.MustRunSynchronized)
			{
				var methodSymbol = result.AnalyzationResult.Symbol;
				var fieldName = GetLockFieldName(methodSymbol, methodSymbol.IsStatic, typeMetadata);
				var newBody = GetUsingAsyncLock(fieldName,
						TriviaList(result.LeadingWhitespaceTrivia), TriviaList(result.EndOfLineTrivia), node.Body)
					.AppendIndent(result.IndentTrivia.ToFullString());
				node = node.WithBody(node.Body
					.WithStatements(SingletonList<StatementSyntax>(newBody)));

				// Remove the Synchronized option from the MethodImpl attribute
				var methodImplAttr = node.AttributeLists.SelectMany(o => o.Attributes).First(o => o.Name.ToString() == "MethodImpl");
				var newMethodImplAttr = (AttributeSyntax)VisitAttribute(methodImplAttr);

				node = node.ReplaceNode(methodImplAttr, newMethodImplAttr);
				newFields.Add(fieldName, GetAsyncLockField(fieldName, methodSymbol.IsStatic, result));
			}

			var transformResult = MethodTransformerResult.Update(node);
			foreach (var newField in newFields.Values)
			{
				transformResult.AddField(newField);
			}
			return transformResult;
		}

		private UsingStatementSyntax GetUsingAsyncLock(string fieldName, SyntaxTriviaList usingLeadingTrivia, SyntaxTriviaList closeParenTrailingTrivia, StatementSyntax statement)
		{
			var expression = AwaitExpression(
				Token(TriviaList(), SyntaxKind.AwaitKeyword, TriviaList(Space)),
				InvocationExpression(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(fieldName),
						IdentifierName(_configuration.AsyncLockMethodName))));
			return UsingStatement(
				Token(usingLeadingTrivia, SyntaxKind.UsingKeyword, TriviaList(Space)),
				Token(TriviaList(), SyntaxKind.OpenParenToken, TriviaList()),
				null,
				expression,
				Token(TriviaList(), SyntaxKind.CloseParenToken, closeParenTrailingTrivia),
				statement);
		}

		private static string GetLockFieldName(ISymbol symbol, bool isStatic, string prefix = "", string postfix = "")
		{
			var fieldName = $"{prefix}{symbol.Name.Trim('_')}{postfix}";
			if (!isStatic)
			{
				fieldName = "_" + fieldName.Substring(0, 1).ToLowerInvariant() + fieldName.Substring(1);
			}
			return fieldName;
		}

		// TODO: helper method
		private static string GetLockFieldName(ISymbol symbol, bool isStatic, ITypeTransformationMetadata typeTransformMetadata, string prefix = "", string postfix = "")
		{
			var fieldName = GetLockFieldName(symbol, isStatic, prefix, postfix);
			var currentIdx = 2;
			var newFieldName = fieldName;
			while (typeTransformMetadata.MemberNames.Contains(newFieldName))
			{
				newFieldName = fieldName + currentIdx;
				currentIdx++;
			}
			return newFieldName;
		}

		private FieldDeclarationSyntax GetAsyncLockField(string fieldName, bool isStatic, IMethodOrAccessorTransformationResult result)
		{
			var list = TokenList(Token(TriviaList(result.LeadingWhitespaceTrivia), SyntaxKind.PrivateKeyword, TriviaList(Space)));
			if (isStatic)
			{
				list = list.Add(Token(TriviaList(), SyntaxKind.StaticKeyword, TriviaList(Space)));
			}
			list = list.Add(Token(TriviaList(), SyntaxKind.ReadOnlyKeyword, TriviaList(Space)));
			var lockType = SyntaxNodeExtensions.ConstructNameSyntax(_configuration.AsyncLockFullTypeName, Space);
			return FieldDeclaration(
					VariableDeclaration(lockType)
						.WithVariables(
							SingletonSeparatedList(
								VariableDeclarator(
										Identifier(TriviaList(), fieldName, TriviaList(Space))
									)
									.WithInitializer(
										EqualsValueClause(
											ObjectCreationExpression(SyntaxNodeExtensions.ConstructNameSyntax(_configuration.AsyncLockFullTypeName))
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
