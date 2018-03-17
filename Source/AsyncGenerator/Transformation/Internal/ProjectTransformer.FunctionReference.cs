using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
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
							returnTypeMismatch = !delReturnType.Equals(returnType); // TODO Generics
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
						returnTypeMismatch = !delReturnType.Equals(returnType); // TODO Generics
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
					if (bodyFuncReferenceResult.UseAsReturnValue)
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

				var conditionalAccessNode = invokeNode.Ancestors()
					.TakeWhile(o => !(o is StatementSyntax))
					.OfType<ConditionalAccessExpressionSyntax>()
					.FirstOrDefault();
				if (conditionalAccessNode != null) // ?. syntax
				{
					var statement = (StatementSyntax)invokeNode.Ancestors().FirstOrDefault(o => o is StatementSyntax);
					var block = statement?.Parent as BlockSyntax;
					if (statement == null || block == null)
					{
						// TODO: convert arrow method/property/function to a normal one
						// TODO: convert to block if there is no block
						node = node.ReplaceNode(conditionalAccessNode,
							conditionalAccessNode.AddAwait(_configuration.ConfigureAwaitArgument));
					}
					else
					{
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

						var lastReturnNode = block.DescendantNodes()
							.Where(o => o.SpanStart >= statement.SpanStart)
							.OfType<ReturnStatementSyntax>()
							.LastOrDefault();

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
							.WithStatements(new SyntaxList<StatementSyntax>()
								.AddRange(newBlock.AppendIndent(typeMetadata.IndentTrivia.ToFullString()).Statements.Skip(index)));

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

						var statements = new SyntaxList<StatementSyntax>()
							.AddRange(newBlock.Statements.Take(index))
							.Add(localVar)
							.Add(ifStatement);
						if (lastReturnNode?.Expression != null)
						{
							// Check if the variable is defined otherwise return default return type value
							if (lastReturnNode.Expression is IdentifierNameSyntax idNode &&
							    statements.OfType<VariableDeclaratorSyntax>().All(o => o.Identifier.ToString() != idNode.Identifier.ValueText))
							{
								lastReturnNode = lastReturnNode.WithExpression(DefaultExpression(funcResult.GetNode().GetReturnType().WithoutTrivia()));
							}
							statements = statements.Add(lastReturnNode);
						}
						node = node.ReplaceNode(block, newBlock.WithStatements(statements));
					}
				}
				else
				{
					node = node.ReplaceNode(invokeNode, invokeNode.AddAwait(_configuration.ConfigureAwaitArgument));
				}
			}
			return node;
		}


		private InvocationExpressionSyntax UpdateTypeAndRunReferenceTransformers(InvocationExpressionSyntax node, IFunctionAnalyzationResult funcResult,
			IFunctionReferenceAnalyzationResult funcReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata,
			Func<MemberAccessExpressionSyntax, INamedTypeSymbol, bool, MemberAccessExpressionSyntax> updateTypeFunc)
		{
			// If the async counterpart is from another type e.g. Thread.Sleep -> Task.Delay, we need to change also the type
			if (!funcReferenceResult.AsyncCounterpartSymbol.IsExtensionMethod &&
			    !funcReferenceResult.AsyncCounterpartSymbol.OriginalDefinition.ContainingType.Equals(
				    funcReferenceResult.ReferenceSymbol.OriginalDefinition.ContainingType) &&
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
			// If the async counterpart is from another type e.g. Thread.Sleep -> Task.Delay, we need to change also the type
			if (!funcReferenceResult.AsyncCounterpartSymbol.IsExtensionMethod &&
			    !funcReferenceResult.AsyncCounterpartSymbol.OriginalDefinition.ContainingType.Equals(
				    funcReferenceResult.ReferenceSymbol.OriginalDefinition.ContainingType))
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
