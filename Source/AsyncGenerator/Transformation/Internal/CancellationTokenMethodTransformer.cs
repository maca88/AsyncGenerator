using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
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

namespace AsyncGenerator.Transformation.Internal
{
	internal class CancellationTokenMethodTransformer : IMethodOrAccessorTransformer
	{
		private IProjectCancellationTokenConfiguration _configuration;
		private const string CancellationTokenParamName = "cancellationToken"; // TODO: handle variable collision for token

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_configuration = configuration.AnalyzeConfiguration.CancellationTokens;
			return Task.CompletedTask;
		}

		// TODO: should we always add guards in anonymous methods?
		//public SyntaxNode Transform(IFunctionTransformationResult functionTransformResult, ITypeTransformationMetadata typeMetadata,
		//	INamespaceTransformationMetadata namespaceMetadata)
		//{
		//	var methodResult = functionTransformResult.AnalyzationResult.GetMethodOrAccessor();
		//	if (methodResult == null || !methodResult.CancellationTokenRequired)
		//	{
		//		return null;
		//	}

		//	var functionNode = functionTransformResult.Transformed;
		//	var functionBody = functionNode.GetFunctionBody() as BlockSyntax; // TODO: support expressions
		//	if (functionBody != null)
		//	{
		//		functionNode = functionNode.ReplaceNode(functionBody,
		//			AddGuards(functionTransformResult, functionBody, functionTransformResult.AnalyzationResult, CancellationTokenParamName));
		//	}

		//	return functionNode;
		//}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.CancellationTokenRequired)
			{
				return MethodTransformerResult.Skip;
			}
			var originalNode = methodResult.GetNode();
			var generationOptions = methodResult.MethodCancellationToken.GetValueOrDefault();
			var methodNode = transformResult.Transformed;
			methodNode = methodNode
				.AddCancellationTokenParameter(CancellationTokenParamName,
					generationOptions.HasFlag(MethodCancellationToken.Optional),
					transformResult.LeadingWhitespaceTrivia,
					transformResult.EndOfLineTrivia);

			var methodBody = methodNode.Body;
			methodNode = methodNode.WithBody(AddGuards(transformResult, methodBody, methodResult, CancellationTokenParamName));

			var originalMethodNode = originalNode as MethodDeclarationSyntax;

			// Add an additional overload if specified
			if (originalMethodNode == null ||
				(
					!generationOptions.HasFlag(MethodCancellationToken.ForwardNone) &&
					!generationOptions.HasFlag(MethodCancellationToken.SealedForwardNone)
				))
			{
				return MethodTransformerResult.Update(methodNode);
			}
			var overloadNode = originalMethodNode
				.ReturnAsTask(namespaceMetadata.TaskConflict)
				.WithTriviaFrom(transformResult.Transformed) // We want to have the sumamry of the transformed node but not the parameter list
				.WithoutAnnotations(transformResult.Annotation)
				.WithIdentifier(Identifier(methodNode.Identifier.ValueText));

			// We can have abstract methods that don't have a body
			if (methodResult.Symbol.IsAbstract)
			{
				// Add the trailing trivia from the semicolon to close paren
				overloadNode = overloadNode
					.WithParameterList(
						overloadNode.ParameterList
							.WithCloseParenToken(
								overloadNode.ParameterList.CloseParenToken.WithTrailingTrivia(overloadNode.SemicolonToken.TrailingTrivia))
					)
					.WithSemicolonToken(default(SyntaxToken));
				methodBody = Block()
					.WithOpenBraceToken(
						Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.OpenBraceToken,
							TriviaList(transformResult.EndOfLineTrivia)))
					.WithCloseBraceToken(
						Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.CloseBraceToken,
							TriviaList(transformResult.EndOfLineTrivia)));
			}

			var tokenArg = Argument(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					IdentifierName("CancellationToken"),
					IdentifierName("None")));
			overloadNode = overloadNode
				.WithBody(methodBody
					.WithStatements(
						SingletonList<StatementSyntax>(
							ReturnStatement(originalMethodNode.ForwardCall(methodResult.Symbol, methodNode.Identifier.ValueText, tokenArg))
								.WithReturnKeyword(
									Token(TriviaList(transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space))
								)
								.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformResult.EndOfLineTrivia)))
						)
					));
			if (generationOptions.HasFlag(MethodCancellationToken.SealedForwardNone))
			{
				if (methodResult.Symbol.IsVirtual)
				{
					// For virtual methods we need to remove the virtual keyword
					overloadNode = overloadNode
						.WithModifiers(TokenList(originalMethodNode.Modifiers.Where(o => !o.IsKind(SyntaxKind.VirtualKeyword))));
				}
				else if (methodResult.Symbol.OverriddenMethod != null)
				{
					// For overrides we need to add the sealed keyword
					overloadNode = overloadNode
						.WithModifiers(originalMethodNode.Modifiers.Add(Token(TriviaList(), SyntaxKind.SealedKeyword, TriviaList(Space))));
				}
				else if (methodResult.Symbol.IsAbstract)
				{
					// For abstract we need to remove the abstract keyword
					overloadNode = overloadNode
						.WithModifiers(TokenList(originalMethodNode.Modifiers.Where(o => !o.IsKind(SyntaxKind.AbstractKeyword))));
				}
			}
			// We need to remove all directives
			while (overloadNode.ContainsDirectives)
			{
				overloadNode = overloadNode.RemoveNode(overloadNode.GetFirstDirective(), SyntaxRemoveOptions.KeepNoTrivia);
			}

			return MethodTransformerResult.Update(methodNode)
				.AddMethod(overloadNode);
		}

		private BlockSyntax AddGuards(IFunctionTransformationResult transformResult, BlockSyntax methodBody,
			IFunctionAnalyzationResult methodResult, string cancellationTokenParamName)
		{
			if (!_configuration.Guards || methodBody == null || methodResult.Faulted)
			{
				return methodBody;
			}

			// Avoid duplicate guards if the first statement also needs a guard
			var afterGuardIndex = methodBody.Statements.Count > methodResult.Preconditions.Count
				? (int?) methodResult.Preconditions.Count
				: null;
			int? afterGuardStatementSpan = null;
			if (afterGuardIndex.HasValue)
			{
				// We have to update the methodBody node in order to have correct spans
				var afterGuardStatement = methodBody.Statements[afterGuardIndex.Value];
				methodBody = methodBody.ReplaceNode(afterGuardStatement,
					afterGuardStatement.WithAdditionalAnnotations(new SyntaxAnnotation("AfterGuardStatement")));
				afterGuardStatementSpan = methodBody.Statements[afterGuardIndex.Value].SpanStart;
			}

			// We need to get all statements that have at least one async invocation without a cancellation token argument, to prepend an extra guard
			var statements = new Dictionary<int, string>();
			foreach (var functionReference in transformResult.TransformedFunctionReferences)
			{
				if (!(functionReference.AnalyzationResult is IBodyFunctionReferenceAnalyzationResult bodyFunctionReference))
				{
					continue;
				}

				if (bodyFunctionReference.GetConversion() != ReferenceConversion.ToAsync ||
				    bodyFunctionReference.PassCancellationToken)
				{
					continue;
				}

				var statement = methodBody
					.GetAnnotatedNodes(functionReference.Annotation)
					.First().Ancestors().OfType<StatementSyntax>().First();
				if (statements.ContainsKey(statement.SpanStart) ||
				    (afterGuardStatementSpan.HasValue && afterGuardStatementSpan.Value == statement.SpanStart))
				{
					continue;
				}

				var annotation = Guid.NewGuid().ToString();
				methodBody = methodBody
					.ReplaceNode(statement, statement.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				statements.Add(statement.SpanStart, annotation);
			}

			var startGuard = methodResult.OmitAsync
				? GetSyncGuard(methodResult, cancellationTokenParamName, transformResult.BodyLeadingWhitespaceTrivia,
					transformResult.EndOfLineTrivia, transformResult.IndentTrivia)
				: GetAsyncGuard(cancellationTokenParamName, transformResult.BodyLeadingWhitespaceTrivia,
					transformResult.EndOfLineTrivia);

			methodBody = methodBody.WithStatements(methodBody.Statements.Insert(methodResult.Preconditions.Count, startGuard));

			// For each statement we need to find the index where is located in the block.
			// TODO: Add support when the parent is not a block syntax
			foreach (var pair in statements)
			{
				var statement = methodBody.GetAnnotatedNodes(pair.Value).OfType<StatementSyntax>().First();
				var parentBlock = statement.Parent as BlockSyntax;
				if (parentBlock == null)
				{
					continue; // Currently not supported
				}

				var index = parentBlock.Statements.IndexOf(statement);
				var newParentBlock = parentBlock
					.WithStatements(parentBlock.Statements
						.Insert(index,
							GetAsyncGuard(cancellationTokenParamName, statement.GetLeadingWhitespace(),
								transformResult.EndOfLineTrivia)));
				methodBody = methodBody
					.ReplaceNode(parentBlock, newParentBlock);
			}

			return methodBody;
		}

		private ExpressionStatementSyntax GetAsyncGuard(string parameterName, SyntaxTrivia leadingWhitespace, SyntaxTrivia endOfLine)
		{
			return ExpressionStatement(
					InvocationExpression(
						MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName(
								Identifier(
									TriviaList(leadingWhitespace),
									parameterName,
									TriviaList())),
							IdentifierName("ThrowIfCancellationRequested"))))
				.WithSemicolonToken(
					Token(
						TriviaList(),
						SyntaxKind.SemicolonToken,
						TriviaList(endOfLine)));
		}


		private StatementSyntax GetSyncGuard(IFunctionAnalyzationResult methodResult, string parameterName, SyntaxTrivia leadingWhitespace, 
			SyntaxTrivia endOfLine, SyntaxTrivia indent)
		{
			return IfStatement(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(parameterName),
						IdentifierName("IsCancellationRequested")),
					Block(
						SingletonList<StatementSyntax>(
							ReturnStatement(
								InvocationExpression(
									MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName("Task"),
										GenericName(Identifier("FromCanceled"))
											.WithTypeArgumentList(
												TypeArgumentList(
													SingletonSeparatedList(
														methodResult.Symbol.ReturnsVoid
															? PredefinedType(Token(SyntaxKind.ObjectKeyword))
															: methodResult.GetNode().GetReturnType().WithoutTrivia())
														))))
									.WithArgumentList(
										ArgumentList(
											SingletonSeparatedList(
												Argument(IdentifierName(parameterName))))))
								.WithReturnKeyword(
									Token(TriviaList(Whitespace(leadingWhitespace.ToFullString() + indent.ToFullString())), SyntaxKind.ReturnKeyword, TriviaList(Space)))
								.WithSemicolonToken(
									Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(endOfLine)))))
						.WithOpenBraceToken(
							Token(TriviaList(leadingWhitespace), SyntaxKind.OpenBraceToken, TriviaList(endOfLine)))
						.WithCloseBraceToken(
							Token(TriviaList(leadingWhitespace), SyntaxKind.CloseBraceToken, TriviaList(endOfLine))))
				.WithIfKeyword(
					Token(TriviaList(leadingWhitespace), SyntaxKind.IfKeyword, TriviaList(Space)))
				.WithCloseParenToken(
					Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(endOfLine)));
		}
	}
}
