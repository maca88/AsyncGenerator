using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = AsyncGenerator.Extensions.Internal.SyntaxNodeExtensions;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Wraps all non taskable returns statements into a <see cref="Task.FromResult{TResult}"/> and conditionally wraps the method body
	/// in a try/catch block (without preconditions) 
	/// </summary>
	internal class ReturnTaskFunctionRewriter : FunctionRewriter
	{
		private readonly IFunctionAnalyzationResult _methodResult;
		private readonly IFunctionTransformationResult _transformResult;
		private readonly INamespaceTransformationMetadata _namespaceMetadata;
		private TypeSyntax _retunTypeSyntax;

		public ReturnTaskFunctionRewriter(IFunctionTransformationResult transformResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			_transformResult = transformResult;
			_methodResult = transformResult.AnalyzationResult;
			_namespaceMetadata = namespaceMetadata;
		}

		protected override SyntaxNode OnVisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			_retunTypeSyntax = !_methodResult.Symbol.ReturnsVoid ? node.ReturnType : null;
			if (!_methodResult.Faulted && 
				(
					(_methodResult.Symbol.ReturnsVoid && node.IsReturnStatementRequired()) || 
					_methodResult.WrapInTryCatch
				)
			)
			{
				node = node.ConvertExpressionBodyToBlock(_transformResult);
			}
			node = (MethodDeclarationSyntax)base.OnVisitMethodDeclaration(node);
			if (node.GetFunctionBody() is BlockSyntax blockBody)
			{
				if (_methodResult is IMethodOrAccessorAnalyzationResult analyzationResult)
				{
					return node.WithBody(RewriteMethodBody(node, blockBody, analyzationResult));
				}
				return node.WithBody(RewriteFunctionBody(blockBody));
			}
			return node;
		}

		protected override SyntaxNode OnVisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			SetupReturnType();
			node =  (AnonymousMethodExpressionSyntax)base.OnVisitAnonymousMethodExpression(node);
			if (node.GetFunctionBody() is BlockSyntax bodyBlock)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		protected override SyntaxNode OnVisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			SetupReturnType();
			node = (ParenthesizedLambdaExpressionSyntax)base.OnVisitParenthesizedLambdaExpression(node);
			if (node.GetFunctionBody() is BlockSyntax bodyBlock)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		protected override SyntaxNode OnVisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			SetupReturnType();
			node = (SimpleLambdaExpressionSyntax)base.OnVisitSimpleLambdaExpression(node);
			if (node.GetFunctionBody() is BlockSyntax bodyBlock)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		protected override SyntaxNode OnVisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
		{
			_retunTypeSyntax = !_methodResult.Symbol.ReturnsVoid ? node.ReturnType : null;
			if (!_methodResult.Faulted &&
			    (
				    (_methodResult.Symbol.ReturnsVoid && node.IsReturnStatementRequired()) ||
				    _methodResult.WrapInTryCatch
			    )
			)
			{
				node = node.ConvertExpressionBodyToBlock(_transformResult);
			}
			node = (LocalFunctionStatementSyntax)base.OnVisitLocalFunctionStatement(node);
			if (node.GetFunctionBody() is BlockSyntax bodyBlock)
			{
				return node.WithBody(RewriteFunctionBody(bodyBlock));
			}
			// TODO: handle arrow methods
			return node;
		}

		public override SyntaxNode VisitCatchClause(CatchClauseSyntax node)
		{
			//// TODO: add a declaration only if there is a throws statement
			//if (node.Declaration == null)
			//{
			//	node = node.WithDeclaration(
			//		CatchDeclaration(IdentifierName(Identifier(TriviaList(), "Exception", TriviaList(Space))))
			//			.WithIdentifier(Identifier("x"))
			//			.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(_eolTrivia))));
			//}

			// We have to add an identifier when only throw is used (e.g. { throw; })
			if (node.Declaration != null && node.Declaration.Identifier.ValueText == null && 
				node.Block?.DescendantNodes().Any(o => o is ThrowStatementSyntax throwStatement && throwStatement.Expression == null) == true)
			{
				node = node.ReplaceNode(node.Declaration, node.Declaration
					.WithType(node.Declaration.Type.WithTrailingTrivia(Space))
					.WithIdentifier(Identifier("x")));
			}

			return base.VisitCatchClause(node);
		}

		public override SyntaxNode Visit(SyntaxNode node)
		{
			if (node == null)
			{
				return null;
			}
			// Skip if the statement is a precondition
			if (_methodResult.Preconditions.Count > 0)
			{
				var statement = node as StatementSyntax;
				// First parent shall be the BlockSyntax (body) and second parent shall be the method
				if (statement != null && statement.Parent?.Parent?.IsKind(RewritingSyntaxKind.GetValueOrDefault()) == true)
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
			if (expression != null && expression.IsReturned() && !expression.GetAnnotations(Annotations.TaskReturned).Any())
			{
				// Before wrapping into a task we need to check if is a conditional expression as we can have a conditional expression and one or both parts can return a Task
				if (node is ConditionalExpressionSyntax conditionalExpression)
				{
					var isWhenTrueTask = conditionalExpression.WhenTrue.GetAnnotations(Annotations.TaskReturned).Any();
					var isWhenFalseTask = conditionalExpression.WhenFalse.GetAnnotations(Annotations.TaskReturned).Any();
					var whenFalse = isWhenFalseTask
						? conditionalExpression.WhenFalse
						: conditionalExpression.WhenFalse.WrapInTaskFromResult(_retunTypeSyntax, _namespaceMetadata.TaskConflict);
					var whenTrue = isWhenTrueTask
						? conditionalExpression.WhenTrue
						: conditionalExpression.WhenTrue.WrapInTaskFromResult(_retunTypeSyntax, _namespaceMetadata.TaskConflict);
					return conditionalExpression
						.WithWhenFalse(whenFalse)
						.WithWhenTrue(whenTrue);
				}
				return expression.WrapInTaskFromResult(_retunTypeSyntax, _namespaceMetadata.TaskConflict);
			}
			return base.Visit(node);
		}

		public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
		{
			// If we have only a return statement rewrite it to a return Task.CompletedTask
			if (node.Expression == null)
			{
				return GetReturnTaskCompleted().WithTriviaFrom(node);
			}
			return base.VisitReturnStatement(node);
		}

		public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node)
		{
			if (_methodResult.Faulted)
			{
				return node;
			}

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

		public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
		{
			if (trivia.IsKind(SyntaxKind.DisabledTextTrivia) && RewritingSyntaxKind.HasValue)
			{
				var node = trivia.Token.Parent;
				var method = node.Ancestors().FirstOrDefault(o => o.IsKind(RewritingSyntaxKind.Value));
				if (method == null) // The directive is outside a method body
				{
					return trivia;
				}
				var startDirective = trivia.Token.Parent.GetLastDirective();
				var endDirective = startDirective.GetNextDirective();
				if (endDirective == null)
				{
					endDirective = startDirective;
					startDirective = trivia.Token.Parent.GetFirstDirective();
				}
				// We have to add a return statement to the disabled region if the compiled region has a return statement
				if (method.DescendantNodes().Any(o =>
					o.IsKind(SyntaxKind.ReturnStatement) &&
					o.SpanStart > startDirective.SpanStart &&
					o.Span.End < endDirective.SpanStart))
				{
					var root = (CompilationUnitSyntax)ParseSyntaxTree($"void Method() {{\n{trivia.ToFullString()}\n}}").GetRoot();
					var body = (BlockSyntax)Visit(root.Members.OfType<MethodDeclarationSyntax>().First().Body);
					if (!body.DescendantNodes().Any(o => o.IsKind(SyntaxKind.ReturnStatement)))
					{
						body = AddReturnStatement(body);
					}
					return DisabledText(string.Join("", body.Statements.Select(o => o.ToFullString())));
				}
			}
			return base.VisitTrivia(trivia);
		}

		private void SetupReturnType()
		{
			if (_methodResult.Symbol.ReturnsVoid)
			{
				var bodyRef = _methodResult.BodyFunctionReferences.FirstOrDefault(o => o.UseAsReturnValue);
				var symbol = bodyRef?.ReferenceSymbol;
				if (symbol?.ReturnsVoid == false)
				{
					_retunTypeSyntax = symbol.ReturnType.CreateTypeSyntax(false,
						_namespaceMetadata.AnalyzationResult.IsIncluded(symbol.ReturnType.ContainingNamespace?.ToString()));
				}
			}
			else
			{
				var returnType = _methodResult.Symbol.ReturnType;
				_retunTypeSyntax = returnType.CreateTypeSyntax(false,
					_namespaceMetadata.AnalyzationResult.IsIncluded(returnType.ContainingNamespace?.ToString()));
			}
		}

		private InvocationExpressionSyntax WrapInTaskFromException(ExpressionSyntax node)
		{
			return InvocationExpression(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						_namespaceMetadata.TaskConflict
							? SyntaxNodeExtensions.ConstructNameSyntax("System.Threading.Tasks.Task").WithLeadingTrivia(node.GetLeadingTrivia())
							: IdentifierName(Identifier(TriviaList(node.GetLeadingTrivia()), nameof(Task), TriviaList())),
						GenericName(
								Identifier("FromException"))
							.WithTypeArgumentList(
								TypeArgumentList(
									SingletonSeparatedList(
										_retunTypeSyntax == null
											? PredefinedType(Token(SyntaxKind.ObjectKeyword))
											: _retunTypeSyntax.WithoutTrivia())))))
				.WithArgumentList(
					ArgumentList(
						SingletonSeparatedList(
							Argument(node.WithoutLeadingTrivia()))));
		}

		private BlockSyntax RewriteMethodBody(MethodDeclarationSyntax node, BlockSyntax body, IMethodOrAccessorAnalyzationResult methodResult)
		{
			if (methodResult.ForwardCall)
			{
				return ForwardCall(node, body);
			}
			return RewriteFunctionBody(body);
		}

		private BlockSyntax RewriteFunctionBody(BlockSyntax body)
		{
			if (_methodResult.Faulted)
			{
				return body;
			}
			if (_methodResult.Symbol.ReturnsVoid && body.IsReturnStatementRequired())
			{
				body = AddReturnStatement(body);
			}
			return _methodResult.WrapInTryCatch ? WrapInsideTryCatch(body, true) : body;
		}

		private BlockSyntax AddReturnStatement(BlockSyntax node)
		{
			var leadingTrivia = node.Statements.Count > 0 
				? node.Statements[0].GetLeadingWhitespace()
				: Whitespace(node.GetLeadingWhitespace().ToFullString() + _transformResult.IndentTrivia.ToFullString());

			return node.AddStatements(GetReturnTaskCompleted().WithLeadingTrivia(TriviaList(leadingTrivia)));
		}

		private ReturnStatementSyntax GetReturnTaskCompleted()
		{
			return ReturnStatement(
				Token(default(SyntaxTriviaList), SyntaxKind.ReturnKeyword, TriviaList(Space)),
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					_namespaceMetadata.TaskConflict
						? SyntaxNodeExtensions.ConstructNameSyntax("System.Threading.Tasks.Task")
						: IdentifierName(nameof(Task)),
					IdentifierName("CompletedTask")),
				Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia))
			);
		}

		private BlockSyntax ForwardCall(MethodDeclarationSyntax methodNode, BlockSyntax bodyBlock)
		{
			var invocation = methodNode.ForwardCall(_methodResult.Symbol, methodNode.Identifier.ValueText);
			var block = Block()
				.WithCloseBraceToken(bodyBlock.CloseBraceToken)
				.WithOpenBraceToken(bodyBlock.OpenBraceToken);
			foreach (var precondition in _methodResult.Preconditions)
			{
				block = block.AddStatements(precondition);
			}

			block = _methodResult.Symbol.ReturnsVoid
				? AddReturnStatement(block.AddStatements(ExpressionStatement(
					invocation.WithLeadingTrivia(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia)),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia)))))
				: block.AddStatements(ReturnStatement(
					Token(TriviaList(_transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					invocation.WrapInTaskFromResult(_retunTypeSyntax, _namespaceMetadata.TaskConflict),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia))));
			return WrapInsideTryCatch(block, false);
		}

		private BlockSyntax WrapInsideTryCatch(BlockSyntax node, bool preserveDirectives)
		{
			var indent = _transformResult.IndentTrivia.ToFullString();
			var innerBodyTrivia = Whitespace(_transformResult.BodyLeadingWhitespaceTrivia.ToFullString());
			var bodyLeadTrivia = TriviaList(_transformResult.LeadingWhitespaceTrivia);
			var eolTrivia = TriviaList(_transformResult.EndOfLineTrivia);

			var tryStatement = TryStatement()
				.WithTryKeyword(Token(bodyLeadTrivia, SyntaxKind.TryKeyword, eolTrivia))
				.WithBlock(Block(node.Statements.Skip(_methodResult.Preconditions.Count))
					.WithOpenBraceToken(Token(bodyLeadTrivia, SyntaxKind.OpenBraceToken, eolTrivia))
					.WithCloseBraceToken(Token(preserveDirectives ? node.CloseBraceToken.LeadingTrivia : bodyLeadTrivia, SyntaxKind.CloseBraceToken, eolTrivia))
				)
				.WithCatches(SingletonList(
					CatchClause()
						.WithCatchKeyword(Token(bodyLeadTrivia, SyntaxKind.CatchKeyword, TriviaList(Space)))
						.WithDeclaration(
							CatchDeclaration(
								_namespaceMetadata.UsingSystem
									? IdentifierName(Identifier(TriviaList(), "Exception", TriviaList(Space)))
									: SyntaxNodeExtensions.ConstructNameSyntax("System.Exception", Space)
								)
								.WithIdentifier(Identifier("ex"))
								.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, eolTrivia))
						)
						.WithBlock(GetCatchBlock(innerBodyTrivia))
				));
			tryStatement = tryStatement.AppendIndent(indent);
			var newStatements = node.Statements.Take(_methodResult.Preconditions.Count).ToList();
			newStatements.Add(tryStatement);

			return Block(newStatements)
				.WithOpenBraceToken(node.OpenBraceToken)
				.WithCloseBraceToken(node.CloseBraceToken
					.WithLeadingTrivia(bodyLeadTrivia));
		}

		private BlockSyntax GetCatchBlock(SyntaxTrivia innerBodyTrivia)
		{
			return Block(
					SingletonList<StatementSyntax>(
						ReturnStatement()
							.WithExpression(
								InvocationExpression(
										MemberAccessExpression(
											SyntaxKind.SimpleMemberAccessExpression,
											_namespaceMetadata.TaskConflict
												? SyntaxNodeExtensions.ConstructNameSyntax("System.Threading.Tasks.Task")
												: IdentifierName(nameof(Task)),
											GenericName(
													Identifier("FromException"))
												.WithTypeArgumentList(
													TypeArgumentList(
														SingletonSeparatedList(
															_retunTypeSyntax == null
																? PredefinedType(Token(SyntaxKind.ObjectKeyword))
																: _retunTypeSyntax.WithoutTrivia())))))
									.WithArgumentList(
										ArgumentList(
											SingletonSeparatedList(
												Argument(
													IdentifierName("ex"))))))
							.WithReturnKeyword(Token(TriviaList(innerBodyTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)))
							.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(_transformResult.EndOfLineTrivia)))
					))
				.WithOpenBraceToken(Token(TriviaList(_transformResult.LeadingWhitespaceTrivia), SyntaxKind.OpenBraceToken, TriviaList(_transformResult.EndOfLineTrivia)))
				.WithCloseBraceToken(Token(TriviaList(_transformResult.LeadingWhitespaceTrivia), SyntaxKind.CloseBraceToken, TriviaList(_transformResult.EndOfLineTrivia)));
		}
	}
}
