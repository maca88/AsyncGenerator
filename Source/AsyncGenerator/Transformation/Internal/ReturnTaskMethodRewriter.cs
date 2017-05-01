using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Wraps all non taskable returns statements into a <see cref="Task.FromResult{TResult}"/> and conditionally wraps the method body
	/// in a try/catch block (without preconditions) 
	/// </summary>
	internal class ReturnTaskMethodRewriter : CSharpSyntaxRewriter
	{
		private readonly IMethodAnalyzationResult _methodResult;
		private readonly MethodTransformationResult _transformResult;
		private SyntaxKind? _rewritingSyntaxKind;

		public ReturnTaskMethodRewriter(MethodTransformationResult transformResult)
		{
			_transformResult = transformResult;
			_methodResult = transformResult.AnalyzationResult;
		}

		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			_rewritingSyntaxKind = node.Kind();
			node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
			var bodyBlock = node.GetFunctionBody() as BlockSyntax;
			if (bodyBlock != null)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			if (_rewritingSyntaxKind.HasValue)
			{
				return node;
			}
			_rewritingSyntaxKind = node.Kind();
			node =  (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node);
			var bodyBlock = node.GetFunctionBody() as BlockSyntax;
			if (bodyBlock != null)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			if (_rewritingSyntaxKind.HasValue)
			{
				return node;
			}
			_rewritingSyntaxKind = node.Kind();
			node = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node);
			var bodyBlock = node.GetFunctionBody() as BlockSyntax;
			if (bodyBlock != null)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			if (_rewritingSyntaxKind.HasValue)
			{
				return node;
			}
			_rewritingSyntaxKind = node.Kind();
			node = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node);
			var bodyBlock = node.GetFunctionBody() as BlockSyntax;
			if (bodyBlock != null)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
		{
			if (_rewritingSyntaxKind.HasValue)
			{
				return node;
			}
			_rewritingSyntaxKind = node.Kind();
			node = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node);
			var bodyBlock = node.GetFunctionBody() as BlockSyntax;
			if (bodyBlock != null)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		//public override SyntaxNode VisitCatchClause(CatchClauseSyntax node)
		//{
		//	// TODO: add a declaration only if there is a throws statement
		//	if (node.Declaration == null)
		//	{
		//		node = node.WithDeclaration(
		//			CatchDeclaration(IdentifierName(Identifier(TriviaList(), "Exception", TriviaList(Space))))
		//				.WithIdentifier(Identifier("x"))
		//				.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(_eolTrivia))));
		//	}
		//	else if (node.Declaration.Identifier.ValueText == null)
		//	{
		//		node = node.ReplaceNode(node.Declaration, node.Declaration.WithIdentifier(Identifier("x")));
		//	}
		//	return base.VisitCatchClause(node);
		//}

		public override SyntaxNode Visit(SyntaxNode node)
		{
			// Skip if the statement is a precondition
			if (_methodResult.Preconditions.Count > 0)
			{
				var statement = node as StatementSyntax;
				// First parent shall be the BlockSyntax (body) and second parent shall be the method
				if (statement != null && statement.Parent?.Parent?.IsKind(_rewritingSyntaxKind.GetValueOrDefault()) == true)
				{
					// Here we can safe cast to BlockSyntax as if there is a StatementSyntax is also a BlockSyntax
					var body = (BlockSyntax)statement.Parent;
					for (var i = 0; i < _methodResult.Preconditions.Count; i++)
					{
						if (body.Statements[i].Equals(statement))
						{
							return node;
						}
					}
				}
			}

			// If the expression is returned and does not return a Task then wrap it into Task.FromResult
			var expression = node as ExpressionSyntax;
			if (expression != null && expression.IsReturned() && !expression.GetAnnotations(_transformResult.TaskReturnedAnnotation).Any())
			{
				return WrapInTaskFromResult(expression);
			}
			return base.Visit(node);
		}

		public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node)
		{
			if (node.Expression == null)
			{
				var catchNode = node.Ancestors().OfType<CatchClauseSyntax>().First();
				return ReturnStatement(
					Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					WrapInTaskFromException(IdentifierName(catchNode.Declaration.Identifier.ValueText)),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia)));
			}

			return ReturnStatement(
					Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					WrapInTaskFromException(node.Expression),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia)))
				.WithReturnKeyword(Token(TriviaList(node.GetLeadingTrivia()), SyntaxKind.ReturnKeyword, TriviaList(Space)));
		}

		private InvocationExpressionSyntax WrapInTaskFromResult(ExpressionSyntax node)
		{
			return InvocationExpression(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(Identifier(TriviaList(node.GetLeadingTrivia()), nameof(Task), TriviaList())),
						GenericName(
								Identifier("FromResult"))
							.WithTypeArgumentList(
								TypeArgumentList(
									SingletonSeparatedList(
										_methodResult.Symbol.ReturnsVoid
											? PredefinedType(Token(SyntaxKind.ObjectKeyword))
											: _methodResult.Node.ReturnType)))))
				.WithArgumentList(
					ArgumentList(
						SingletonSeparatedList(
							Argument(node.WithoutLeadingTrivia()))));
		}

		private InvocationExpressionSyntax WrapInTaskFromException(ExpressionSyntax node)
		{
			return InvocationExpression(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(Identifier(TriviaList(), nameof(Task), TriviaList())),
						GenericName(
								Identifier("FromException"))
							.WithTypeArgumentList(
								TypeArgumentList(
									SingletonSeparatedList(
										_methodResult.Symbol.ReturnsVoid
											? PredefinedType(Token(SyntaxKind.ObjectKeyword))
											: _methodResult.Node.ReturnType)))))
				.WithArgumentList(
					ArgumentList(
						SingletonSeparatedList(
							Argument(node.WithoutLeadingTrivia()))));
		}

		private BlockSyntax RewriteFunctionBody(BlockSyntax body)
		{
			if (_methodResult.Symbol.ReturnsVoid && !body.EndsWithReturnStatement())
			{
				return AddReturnStatement(body);
			}
			return _methodResult.WrapInTryCatch ? WrapInsideTryCatch(body) : body;
		}

		private BlockSyntax AddReturnStatement(BlockSyntax node)
		{
			return node.AddStatements(
				ReturnStatement(
					Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(nameof(Task)),
						IdentifierName("CompletedTask")),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia))
				));
		}

		private BlockSyntax WrapInsideTryCatch(BlockSyntax node)
		{
			//var innerBodyTrivia = Whitespace(_transformResult.BodyLeadingWhitespaceTrivia.ToFullString() + _transformResult.IndentTrivia.ToFullString());
			var tryStatement = TryStatement()
				.WithTryKeyword(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.TryKeyword, TriviaList(_transformResult.EndOfLineTrivia)))
				.WithBlock(Block(node.Statements.Skip(_methodResult.Preconditions.Count))
					.WithOpenBraceToken(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.OpenBraceToken, TriviaList(_transformResult.EndOfLineTrivia)))
					.WithCloseBraceToken(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.CloseBraceToken, TriviaList(_transformResult.EndOfLineTrivia)))
				)
				.WithCatches(SingletonList(
					CatchClause()
						.WithCatchKeyword(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.CatchKeyword, TriviaList(Space)))
						.WithDeclaration(
							CatchDeclaration(IdentifierName(Identifier(TriviaList(), "Exception", TriviaList(Space))))
								.WithIdentifier(Identifier("ex"))
								.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(_transformResult.EndOfLineTrivia)))
						)
						.WithBlock(GetCatchBlock(/*innerBodyTrivia*/))
				));
			// To indent the wrapped statement we will use the NormalizeWhitespace method as the indentation process is too complex to handle manually
			// For using the NormalizeWhitespace we need to place the wrapped body in the original document node to get the right indentation
			var annotation = Guid.NewGuid().ToString();
			var docNode = _methodResult.Node.Ancestors().OfType<CompilationUnitSyntax>().First();
			docNode = docNode.ReplaceNode(_methodResult.Node, _methodResult.Node
				.WithBody(_methodResult.Node.Body
					.WithStatements(SingletonList<StatementSyntax>(tryStatement
						.WithAdditionalAnnotations(new SyntaxAnnotation(annotation))))));
			tryStatement = docNode
				.NormalizeWhitespace(
					_transformResult.IndentTrivia.ToFullString(),
					_transformResult.EndOfLineTrivia.ToString())
				.GetAnnotatedNodes(annotation)
				.OfType<TryStatementSyntax>()
				.First();

			var newStatements = node.Statements.Take(_methodResult.Preconditions.Count).ToList();
			newStatements.Add(tryStatement);

			return Block(newStatements)
				.WithOpenBraceToken(node.OpenBraceToken)
				.WithCloseBraceToken(node.CloseBraceToken);
		}

		private BlockSyntax GetCatchBlock(/*SyntaxTrivia innerBodyTrivia*/)
		{
			return Block(
					SingletonList<StatementSyntax>(
						ReturnStatement()
							.WithExpression(
								InvocationExpression(
										MemberAccessExpression(
											SyntaxKind.SimpleMemberAccessExpression,
											IdentifierName(nameof(Task)),
											GenericName(
													Identifier("FromException"))
												.WithTypeArgumentList(
													TypeArgumentList(
														SingletonSeparatedList(
															_methodResult.Symbol.ReturnsVoid
																? PredefinedType(Token(SyntaxKind.ObjectKeyword))
																: _methodResult.Node.ReturnType)))))
									.WithArgumentList(
										ArgumentList(
											SingletonSeparatedList(
												Argument(
													IdentifierName("ex"))))))
							.WithReturnKeyword(Token(TriviaList(/*innerBodyTrivia*/), SyntaxKind.ReturnKeyword, TriviaList(Space)))
							.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia)))
					))
				.WithOpenBraceToken(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.OpenBraceToken, TriviaList(_transformResult.EndOfLineTrivia)))
				.WithCloseBraceToken(Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.CloseBraceToken, TriviaList(_transformResult.EndOfLineTrivia)));
		}
	}
}
