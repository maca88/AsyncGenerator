using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private T TransformFunctionReference<T>(T node, IFunctionAnalyzationResult funcResult, FunctionReferenceTransformationResult transfromReference, 
			ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
			where T : SyntaxNode
		{
			var nameNode = node.GetAnnotatedNodes(transfromReference.Annotation).OfType<SimpleNameSyntax>().First();
			var funReferenceResult = transfromReference.AnalyzationResult;
			var bodyFuncReferenceResult = funReferenceResult as IBodyFunctionReferenceAnalyzationResult;
			var newNameNode = nameNode
				.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
				.WithTriviaFrom(nameNode);
			transfromReference.Transformed = newNameNode;

			var cancellationTokenParamName = funcResult.GetMethodOrAccessor().CancellationTokenRequired ? "cancellationToken" : null; // TODO: remove

			// If we have a cref change the name to the async counterpart and add/update arguments
			if (bodyFuncReferenceResult == null)
			{
				if (funReferenceResult.IsCref)
				{
					var crefNode = (NameMemberCrefSyntax)nameNode.Parent;
					var paramList = new List<CrefParameterSyntax>();
					// If the cref has already the parameters set then use them
					if (crefNode.Parameters != null)
					{
						paramList.AddRange(crefNode.Parameters.Parameters);
						// If the external async counterpart has a cancellation token, add it
						if (funReferenceResult.AsyncCounterpartFunction == null &&
						    funReferenceResult.ReferenceSymbol.Parameters.Length <
						    funReferenceResult.AsyncCounterpartSymbol.Parameters.Length)
						{
							paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
						}
					}
					else
					{
						// We have to add the parameters to avoid ambiguity
						var asyncSymbol = funReferenceResult.AsyncCounterpartSymbol;
						paramList.AddRange(asyncSymbol.Parameters
							.Select(o => CrefParameter(o.Type
								.CreateTypeSyntax(true, namespaceMetadata.AnalyzationResult.IsIncluded(o.Type.ContainingNamespace?.ToString())))));
					}

					// If the async counterpart is internal and a token is required add a token parameter
					if (funReferenceResult.AsyncCounterpartFunction?.GetMethodOrAccessor()?.CancellationTokenRequired == true)
					{
						paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
					}

					node = node.ReplaceNestedNodes(
						crefNode.Parent as QualifiedCrefSyntax,
						crefNode,
						crefNode
							.ReplaceNode(nameNode, newNameNode)
							.WithParameters(CrefParameterList(SeparatedList(paramList))),
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode, funcResult, funReferenceResult, namespaceMetadata,
							(type, fullName) => rootNode.WithContainer(type.CreateTypeSyntax(true, fullName).WithTriviaFrom(rootNode.Container))),
						childNode => RunReferenceTransformers(childNode, funcResult, funReferenceResult, namespaceMetadata)
					);
				}
				else if (funReferenceResult.IsNameOf)
				{
					node = node.ReplaceNestedNodes(
						nameNode.Parent as MemberAccessExpressionSyntax,
						nameNode,
						newNameNode,
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode, funcResult, funReferenceResult, namespaceMetadata,
							(type, fullName) => rootNode.WithExpression(type.CreateTypeSyntax(false, fullName).WithTriviaFrom(rootNode.Expression))),
						childNode => RunReferenceTransformers(childNode, funcResult, funReferenceResult, namespaceMetadata)
					);
				}
				return node;

			}
			// If we have a method passed as an argument we need to check if we have to wrap it inside a function
			if (bodyFuncReferenceResult.AsyncDelegateArgument != null)
			{
				if (bodyFuncReferenceResult.WrapInsideFunction)
				{
					// TODO: move to analyze step
					var argumentNode = nameNode.Ancestors().OfType<ArgumentSyntax>().First();
					var delReturnType = (INamedTypeSymbol)bodyFuncReferenceResult.AsyncDelegateArgument.ReturnType;
					var returnType = bodyFuncReferenceResult.AsyncCounterpartSymbol.ReturnType;
					bool returnTypeMismatch;
					if (bodyFuncReferenceResult.ReferenceFunction != null)
					{
						var refMethod = bodyFuncReferenceResult.ReferenceFunction as IMethodAnalyzationResult;
						if (refMethod != null && refMethod.PreserveReturnType)
						{
							returnTypeMismatch = !delReturnType.EqualTo(returnType); // TODO Generics
						}
						else if (delReturnType.IsGenericType) // Generic Task
						{
							returnTypeMismatch = delReturnType.TypeArguments.First().IsAwaitRequired(returnType);
						}
						else
						{
							returnTypeMismatch = delReturnType.IsAwaitRequired(returnType);
						}
					}
					else
					{
						returnTypeMismatch = !delReturnType.EqualTo(returnType); // TODO Generics
					}

					var newArgumentExpression = argumentNode.Expression
						.ReplaceNestedNodes(
							nameNode.Parent as MemberAccessExpressionSyntax,
							nameNode,
							newNameNode,
							rootNode => UpdateTypeAndRunReferenceTransformers(rootNode, funcResult, funReferenceResult, namespaceMetadata,
								(type, fullName) => rootNode.WithExpression(type.CreateTypeSyntax(false, fullName))),
							childNode => RunReferenceTransformers(childNode, funcResult, funReferenceResult, namespaceMetadata)
						)
						.WrapInsideFunction(bodyFuncReferenceResult.AsyncDelegateArgument, returnTypeMismatch,
							namespaceMetadata.TaskConflict,
							invocation => invocation.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult));
					node = node
						.ReplaceNode(argumentNode.Expression, newArgumentExpression);
				}
				else
				{
					node = node.ReplaceNestedNodes(
						nameNode.Parent as MemberAccessExpressionSyntax,
						nameNode,
						newNameNode,
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode, funcResult, funReferenceResult, namespaceMetadata,
							(type, fullName) => rootNode.WithExpression(type.CreateTypeSyntax(false, fullName))),
						childNode => RunReferenceTransformers(childNode, funcResult, funReferenceResult, namespaceMetadata)
					);
				}
				return node;
			}

			InvocationExpressionSyntax invokeNode = null;
			var isAccessor = bodyFuncReferenceResult.ReferenceSymbol.IsAccessor();
			if (!isAccessor && funReferenceResult.ReferenceNode.IsKind(SyntaxKind.InvocationExpression))
			{
				invokeNode = nameNode.Ancestors().OfType<InvocationExpressionSyntax>().First();
			}

			if (!bodyFuncReferenceResult.AwaitInvocation)
			{
				// An arrow method does not have a statement
				var statement = nameNode.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
				var statementInParentFunction = nameNode.Ancestors().TakeWhile(o => !o.Equals(statement)).Any(o => o.IsFunction());
				var newNode = (SyntaxNode)statement ?? node;

				if (invokeNode != null)
				{
					newNode = newNode.ReplaceNestedNodes(
						invokeNode,
						nameNode,
						newNameNode,
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode
								.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult),
							funcResult, funReferenceResult, namespaceMetadata,
							(memberNode, type, fullName) => memberNode.WithExpression(type.CreateTypeSyntax(true, fullName).WithTriviaFrom(memberNode.Expression)))
					);
				}
				else if (isAccessor)
				{
					newNode = ConvertAccessor(newNode, nameNode, newNameNode, cancellationTokenParamName, bodyFuncReferenceResult, 
						invNode => UpdateTypeAndRunReferenceTransformers(invNode, funcResult, funReferenceResult, namespaceMetadata,
							(memberNode, type, fullName) => memberNode.WithExpression(type.CreateTypeSyntax(true, fullName).WithTriviaFrom(memberNode.Expression))));
				}
				else
				{
					newNode = newNode.ReplaceNestedNodes(
						nameNode.Parent as MemberAccessExpressionSyntax,
						nameNode,
						newNameNode,
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode, funcResult, funReferenceResult, namespaceMetadata,
							(type, fullName) => rootNode.WithExpression(type.CreateTypeSyntax(false, fullName).WithTriviaFrom(rootNode.Expression))),
						childNode => RunReferenceTransformers(childNode, funcResult, funReferenceResult, namespaceMetadata)
					);
				}

				if (statement != null && !statement.IsKind(SyntaxKind.LocalFunctionStatement))
				{
					// Skip adding return statement for arrow functions
					if (bodyFuncReferenceResult.UseAsReturnValue && !statementInParentFunction)
					{
						newNode = ((StatementSyntax) newNode).ToReturnStatement();
					}
					node = node
						.ReplaceNode(statement, newNode);
				}
				else
				{
					node = (T)newNode;
				}
			}
			else
			{
				// We need to annotate the invocation node because of the AddAwait method as it needs the parent node
				var invokeAnnotation = Guid.NewGuid().ToString();
				if (isAccessor)
				{
					node = ConvertAccessor(node, nameNode, newNameNode, cancellationTokenParamName, bodyFuncReferenceResult, invNode =>
						UpdateTypeAndRunReferenceTransformers(invNode, funcResult, funReferenceResult, namespaceMetadata,
								(memberNode, type, fullName) => memberNode.WithExpression(type.CreateTypeSyntax(true, fullName).WithTriviaFrom(memberNode.Expression)))
							.WithAdditionalAnnotations(new SyntaxAnnotation(invokeAnnotation))
					);
				}
				else
				{
					node = node.ReplaceNestedNodes(
						invokeNode,
						nameNode,
						newNameNode,
						rootNode => UpdateTypeAndRunReferenceTransformers(rootNode
									.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult),
								funcResult, funReferenceResult, namespaceMetadata,
								(memberNode, type, fullName) => memberNode.WithExpression(type.CreateTypeSyntax(true, fullName).WithTriviaFrom(memberNode.Expression)))
							.WithAdditionalAnnotations(new SyntaxAnnotation(invokeAnnotation))
					);
				}

				invokeNode = node.GetAnnotatedNodes(invokeAnnotation).OfType<InvocationExpressionSyntax>().First();

				// Check if the invocation has a ?.
				var conditionalAccessNode = invokeNode.Ancestors()
					.TakeWhile(o => !(o is StatementSyntax))
					.OfType<ConditionalAccessExpressionSyntax>()
					.FirstOrDefault(o => o.WhenNotNull.Contains(invokeNode));
				if (conditionalAccessNode != null) // ?. syntax
				{
					// We have to find out which strategy to use, if we have a non assignable expression, we are force to use if statements
					// otherwise a ternary condition will be used
					if (!conditionalAccessNode.Parent.IsKind(SyntaxKind.ExpressionStatement) || !invokeNode.Equals(conditionalAccessNode.WhenNotNull))
					{
						node = TransformConditionalAccessToConditionalExpressions(node, nameNode, funReferenceResult, typeMetadata,
							conditionalAccessNode, invokeNode);
					}
					else
					{
						node = TransformConditionalAccessToIfStatements(node, nameNode, typeMetadata, conditionalAccessNode, invokeNode);
					}
				}
				else
				{
					node = node.ReplaceNode(invokeNode, invokeNode.AddAwait(_configuration.ConfigureAwaitArgument));
				}
			}
			return node;
		}

		private T TransformConditionalAccessToConditionalExpressions<T>(
			T node,
			SimpleNameSyntax nameNode,
			IFunctionReferenceAnalyzationResult funReferenceResult,
			ITypeTransformationMetadata typeMetadata,
			ConditionalAccessExpressionSyntax conditionalAccessNode,
			InvocationExpressionSyntax invokeNode) where T : SyntaxNode
		{
			// TODO: we should check the async symbol instead
			var returnType = funReferenceResult.ReferenceSymbol.ReturnType;
			var type = returnType.CreateTypeSyntax();
			var canSkipCast = !returnType.IsValueType && !returnType.IsNullable();
			if (returnType.IsValueType && !returnType.IsNullable())
			{
				type = NullableType(type);
			}

			ExpressionSyntax whenNotNullNode = null;
			if (invokeNode.Parent is MemberAccessExpressionSyntax memberAccessParent)
			{
				whenNotNullNode = conditionalAccessNode.WhenNotNull
					.ReplaceNode(memberAccessParent,
						MemberBindingExpression(Token(SyntaxKind.DotToken), memberAccessParent.Name));
			}
			else if (invokeNode.Parent is ElementAccessExpressionSyntax elementAccessParent)
			{
				whenNotNullNode = conditionalAccessNode.WhenNotNull
					.ReplaceNode(elementAccessParent,
						ElementBindingExpression(elementAccessParent.ArgumentList));
			}

			var valueNode = conditionalAccessNode.Expression.WithoutTrivia();
			StatementSyntax variableStatement = null;
			BlockSyntax statementBlock = null;
			var statementIndex = 0;
			// We have to save the value in a variable when the expression is an invocation, index accessor or property in
			// order to prevent double calls
			// TODO: find a more robust solution
			if (!(conditionalAccessNode.Expression is SimpleNameSyntax simpleName) || char.IsUpper(simpleName.ToString()[0]))
			{
				var statement = (StatementSyntax)conditionalAccessNode.Ancestors().FirstOrDefault(o => o is StatementSyntax);
				if (statement == null || !(statement.Parent is BlockSyntax block))
				{
					// TODO: convert arrow method/property/function to a normal one
					// TODO: convert to block if there is no block
					throw new NotSupportedException(
						$"Arrow method with null-conditional is not supported. Node: {conditionalAccessNode}");
				}

				var leadingTrivia = statement.GetLeadingTrivia();
				var fnName = nameNode.Identifier.ValueText;
				statementIndex = block.Statements.IndexOf(statement);
				statementBlock = block;
				// TODO: handle name collisions
				var variableName = $"{char.ToLowerInvariant(fnName[0])}{fnName.Substring(1)}{statementIndex}";
				variableStatement = LocalDeclarationStatement(
						VariableDeclaration(
							IdentifierName(Identifier(leadingTrivia, "var", TriviaList(Space))),
							SingletonSeparatedList(
								VariableDeclarator(
										Identifier(TriviaList(), variableName, TriviaList(Space)))
									.WithInitializer(
										EqualsValueClause(valueNode)
											.WithEqualsToken(Token(TriviaList(), SyntaxKind.EqualsToken, TriviaList(Space)))
									)
							)))
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(typeMetadata.EndOfLineTrivia)));

				valueNode = IdentifierName(variableName);
			}

			var invocationAnnotation = Guid.NewGuid().ToString();
			var nullNode = LiteralExpression(
				SyntaxKind.NullLiteralExpression,
				Token(TriviaList(), SyntaxKind.NullKeyword, TriviaList(Space)));
			var ifNullCondition = BinaryExpression(
					SyntaxKind.EqualsExpression,
					valueNode.WithTrailingTrivia(TriviaList(Space)),
					nullNode)
				.WithOperatorToken(
					Token(TriviaList(), SyntaxKind.EqualsEqualsToken, TriviaList(Space)));

			ExpressionSyntax wrappedNode = ParenthesizedExpression(
				ConditionalExpression(
						ifNullCondition,
						canSkipCast
							? (ExpressionSyntax) nullNode
							: CastExpression(
								Token(SyntaxKind.OpenParenToken),
								type,
								Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(Space)),
								nullNode),
						InvocationExpression(
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									valueNode,
									((MemberBindingExpressionSyntax) invokeNode.Expression).Name))
							.WithAdditionalAnnotations(new SyntaxAnnotation(invocationAnnotation))
							.WithArgumentList(invokeNode.ArgumentList.WithoutTrailingTrivia())
					)
					.WithColonToken(Token(TriviaList(), SyntaxKind.ColonToken, TriviaList(Space)))
					.WithQuestionToken(Token(TriviaList(), SyntaxKind.QuestionToken, TriviaList(Space)))
			);

			if (whenNotNullNode != null)
			{
				wrappedNode = conditionalAccessNode
					.WithExpression(wrappedNode)
					.WithWhenNotNull(whenNotNullNode);
			}

			wrappedNode = wrappedNode.WithTriviaFrom(conditionalAccessNode);
			invokeNode = (InvocationExpressionSyntax) wrappedNode.GetAnnotatedNodes(invocationAnnotation).First();
			wrappedNode = wrappedNode.ReplaceNode(invokeNode, invokeNode.AddAwait(_configuration.ConfigureAwaitArgument));
			if (statementBlock != null)
			{
				var newBlock = statementBlock.ReplaceNode(conditionalAccessNode, wrappedNode);
				newBlock = newBlock.WithStatements(newBlock.Statements.Insert(statementIndex, variableStatement));

				return node.ReplaceNode(statementBlock, newBlock);
			}

			return node.ReplaceNode(conditionalAccessNode, wrappedNode);
		}

		private T TransformConditionalAccessToIfStatements<T>(
			T node,
			SimpleNameSyntax nameNode,
			ITypeTransformationMetadata typeMetadata,
			ConditionalAccessExpressionSyntax conditionalAccessNode,
			InvocationExpressionSyntax invokeNode) where T : SyntaxNode
		{
			var statement = (StatementSyntax)conditionalAccessNode.Ancestors().FirstOrDefault(o => o is StatementSyntax);
			if (statement == null || !(statement.Parent is BlockSyntax block))
			{
				// TODO: convert arrow method/property/function to a normal one
				// TODO: convert to block if there is no block
				throw new NotSupportedException(
					$"Arrow method with null-conditional is not supported. Node: {conditionalAccessNode}");
			}

			var fnName = nameNode.Identifier.ValueText;
			// TODO: handle name collisions
			var variableName = $"{char.ToLowerInvariant(fnName[0])}{fnName.Substring(1)}Task";
			var leadingTrivia = statement.GetLeadingTrivia();
			var newConditionalAccessNode = ConditionalAccessExpression(
					conditionalAccessNode.Expression,
					invokeNode)
				.WithTriviaFrom(conditionalAccessNode);
			var localVar = LocalDeclarationStatement(
				VariableDeclaration(
					IdentifierName(Identifier(leadingTrivia, "var", TriviaList(Space))),
					SingletonSeparatedList(
						VariableDeclarator(
								Identifier(TriviaList(), variableName, TriviaList(Space)))
							.WithInitializer(
								EqualsValueClause(newConditionalAccessNode.WithoutTrivia())
									.WithEqualsToken(Token(TriviaList(), SyntaxKind.EqualsToken, TriviaList(Space)))
							)
					)))
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(typeMetadata.EndOfLineTrivia)));
			var index = block.Statements.IndexOf(statement);

			var variableAnnotation = Guid.NewGuid().ToString();
			var newBlock = block.ReplaceNode(conditionalAccessNode,
				conditionalAccessNode.WhenNotNull.ReplaceNode(invokeNode,
					IdentifierName(variableName)
						.WithAdditionalAnnotations(new SyntaxAnnotation(variableAnnotation))
						.WithLeadingTrivia(conditionalAccessNode.GetLeadingTrivia())
						.WithTrailingTrivia(conditionalAccessNode.GetTrailingTrivia())
				));

			var variable = newBlock.GetAnnotatedNodes(variableAnnotation).OfType<IdentifierNameSyntax>().First();
			newBlock = newBlock.ReplaceNode(variable, variable.AddAwait(_configuration.ConfigureAwaitArgument));

			var ifBlock = Block()
				.WithOpenBraceToken(
					Token(TriviaList(leadingTrivia), SyntaxKind.OpenBraceToken, TriviaList(typeMetadata.EndOfLineTrivia)))
				.WithCloseBraceToken(
					Token(TriviaList(leadingTrivia), SyntaxKind.CloseBraceToken, TriviaList(typeMetadata.EndOfLineTrivia)))
				.WithStatements(SingletonList(newBlock.Statements[index].AppendIndent(typeMetadata.IndentTrivia.ToFullString())));

			var ifStatement = IfStatement(
					BinaryExpression(
							SyntaxKind.NotEqualsExpression,
							IdentifierName(Identifier(TriviaList(), variableName, TriviaList(Space))),
							LiteralExpression(SyntaxKind.NullLiteralExpression))
						.WithOperatorToken(
							Token(TriviaList(), SyntaxKind.ExclamationEqualsToken, TriviaList(Space))),
					ifBlock
				)
				.WithIfKeyword(
					Token(TriviaList(leadingTrivia), SyntaxKind.IfKeyword, TriviaList(Space)))
				.WithCloseParenToken(
					Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(typeMetadata.EndOfLineTrivia)));

			newBlock = block.WithStatements(
				block.Statements
					.RemoveAt(index)
					.InsertRange(index, new StatementSyntax[] {localVar, ifStatement})
			);

			return node.ReplaceNode(block, newBlock);
		}

		private InvocationExpressionSyntax UpdateTypeAndRunReferenceTransformers(InvocationExpressionSyntax node, IFunctionAnalyzationResult funcResult,
			IFunctionReferenceAnalyzationResult funcReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata,
			Func<MemberAccessExpressionSyntax, INamedTypeSymbol, bool, MemberAccessExpressionSyntax> updateTypeFunc)
		{
			var asyncContainingType = funcReferenceResult.AsyncCounterpartSymbol.OriginalDefinition.ContainingType;
			var syncContainingType = funcReferenceResult.ReferenceSymbol.OriginalDefinition.ContainingType;
			// If the async counterpart is from another type that is static e.g. Thread.Sleep -> Task.Delay, we need to change also the type
			if (!funcReferenceResult.AsyncCounterpartSymbol.IsExtensionMethod &&
			    !asyncContainingType.EqualTo(syncContainingType) &&
			    funcReferenceResult.ReferenceSymbol.IsStatic &&
				node.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				var type = funcReferenceResult.AsyncCounterpartSymbol.ContainingType;
				node = node.WithExpression(updateTypeFunc(memberAccess, type, 
					namespaceMetadata.AnalyzationResult.IsIncluded(type.ContainingNamespace?.ToString())));
			}
			return RunReferenceTransformers(node, funcResult, funcReferenceResult, namespaceMetadata);
		}

		private T UpdateTypeAndRunReferenceTransformers<T>(T node, IFunctionAnalyzationResult funcResult, IFunctionReferenceAnalyzationResult funcReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata, Func<INamedTypeSymbol, bool, T> updateTypeFunc)
			where T : SyntaxNode
		{
			var asyncContainingType = funcReferenceResult.AsyncCounterpartSymbol.OriginalDefinition.ContainingType;
			var syncContainingType = funcReferenceResult.ReferenceSymbol.OriginalDefinition.ContainingType;
			// If the async counterpart is from another type that is static e.g. Thread.Sleep -> Task.Delay, we need to change also the type
			if (!funcReferenceResult.AsyncCounterpartSymbol.IsExtensionMethod &&
			    !asyncContainingType.EqualTo(syncContainingType) &&
			    funcReferenceResult.ReferenceSymbol.IsStatic)
			{
				var type = funcReferenceResult.AsyncCounterpartSymbol.ContainingType;
				node = updateTypeFunc(type, namespaceMetadata.AnalyzationResult.IsIncluded(type.ContainingNamespace?.ToString()));
			}
			return RunReferenceTransformers(node, funcResult, funcReferenceResult, namespaceMetadata);
		}

		private T RunReferenceTransformers<T>(T node, IFunctionAnalyzationResult funcResult, IFunctionReferenceAnalyzationResult funcReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata)
			where T : SyntaxNode
		{
			foreach (var transformer in _configuration.FunctionReferenceTransformers)
			{
				node = (T)transformer.TransformFunctionReference(node, funcResult, funcReferenceResult, namespaceMetadata) ?? node;
			}
			return node;
		}

		private static TNode ConvertAccessor<TNode>(TNode node, SimpleNameSyntax nameNode, SimpleNameSyntax newNameNode, 
			string cancellationTokenParamName, IBodyFunctionReferenceAnalyzationResult bodyFuncReferenceResult,
			Func<InvocationExpressionSyntax, InvocationExpressionSyntax> alterFn = null) where TNode : SyntaxNode
		{
			var expressionNode = nameNode.GetAccessorExpression();
			var invocation = InvocationExpression(
					expressionNode.IsKind(SyntaxKind.SimpleAssignmentExpression)
						? newNameNode.WithoutTrivia().WithoutAnnotations()
						: expressionNode.ReplaceNode(nameNode, newNameNode).WithoutTrivia().WithoutAnnotations()
				)
				.WithTriviaFrom(expressionNode)
				.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult)
				.WithAdditionalAnnotations(nameNode.GetAnnotations(Annotations.TaskReturned))
				.AddAssignedValueAsArgument(expressionNode);
			if (alterFn != null)
			{
				invocation = alterFn(invocation);
			}
			return node.ReplaceNode(expressionNode, invocation);
		}
	}
}
