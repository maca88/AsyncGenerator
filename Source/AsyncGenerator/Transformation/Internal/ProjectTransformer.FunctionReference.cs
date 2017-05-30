using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private T TransformFunctionReference<T>(T node, IFunctionAnalyzationResult funcResult, FunctionReferenceTransformationResult transfromReference, INamespaceTransformationMetadata namespaceMetadata)
			where T : SyntaxNode
		{
			var nameNode = node.GetAnnotatedNodes(transfromReference.Annotation).OfType<SimpleNameSyntax>().First();
			var funReferenceResult = transfromReference.AnalyzationResult;
			var bodyFuncReferenceResult = funReferenceResult as IBodyFunctionReferenceAnalyzationResult;
			var newNameNode = nameNode
				.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
				.WithTriviaFrom(nameNode);
			transfromReference.Transformed = newNameNode;

			var cancellationTokenParamName = funcResult.GetMethod().CancellationTokenRequired ? "cancellationToken" : null; // TODO: remove

			// If we have a cref change the name to the async counterpart and add/update arguments
			if (bodyFuncReferenceResult == null)
			{
				var crefNode = (NameMemberCrefSyntax)nameNode.Parent;
				var paramList = new List<CrefParameterSyntax>();
				// If the cref has already the parameters set then use them
				if (crefNode.Parameters != null)
				{
					paramList.AddRange(crefNode.Parameters.Parameters);
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
				if (funReferenceResult.AsyncCounterpartFunction?.GetMethod()?.CancellationTokenRequired == true)
				{
					paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
				}
				node = node
						.ReplaceNode(crefNode, crefNode
							.ReplaceNode(nameNode, newNameNode)
							.WithParameters(CrefParameterList(SeparatedList(paramList))))
					;
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
						.ReplaceNode(nameNode, newNameNode)
						.WrapInsideFunction(bodyFuncReferenceResult.AsyncDelegateArgument, returnTypeMismatch,
							namespaceMetadata.TaskConflict,
							invocation => invocation.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult));

					node = node
						.ReplaceNode(argumentNode.Expression, newArgumentExpression);
				}
				else
				{
					node = node
						.ReplaceNode(nameNode, newNameNode);
				}
				return node;
			}

			InvocationExpressionSyntax invokeNode = null;
			if (bodyFuncReferenceResult.PassCancellationToken || bodyFuncReferenceResult.AwaitInvocation)
			{
				invokeNode = nameNode.Ancestors().OfType<InvocationExpressionSyntax>().First();
			}

			if (!bodyFuncReferenceResult.AwaitInvocation)
			{
				var statement = nameNode.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
				// An arrow method does not have a statement
				if (statement == null)
				{
					if (invokeNode != null)
					{
						node = node.ReplaceNode(invokeNode, invokeNode
							.ReplaceNode(nameNode, newNameNode)
							.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult));
					}
					else
					{
						node = node
							.ReplaceNode(nameNode, newNameNode);
					}
				}
				else
				{
					StatementSyntax newStatement;
					if (invokeNode != null)
					{
						newStatement = statement.ReplaceNode(invokeNode, invokeNode
							.ReplaceNode(nameNode, newNameNode)
							.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult));
					}
					else
					{
						newStatement = statement.ReplaceNode(nameNode, newNameNode);
					}

					if (bodyFuncReferenceResult.UseAsReturnValue)
					{
						newStatement = newStatement.ToReturnStatement();
					}

					node = node
						.ReplaceNode(statement, newStatement);
				}
			}
			else
			{
				// We need to annotate the invocation node because of the AddAwait method as it needs the parent node
				var invokeAnnotation = Guid.NewGuid().ToString();
				node = node
					.ReplaceNode(invokeNode, invokeNode
						.ReplaceNode(nameNode, newNameNode)
						.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult)
						.WithAdditionalAnnotations(new SyntaxAnnotation(invokeAnnotation))
					);
				invokeNode = node.GetAnnotatedNodes(invokeAnnotation).OfType<InvocationExpressionSyntax>().First();
				node = node.ReplaceNode(invokeNode, invokeNode.AddAwait(_configuration.ConfigureAwaitArgument));
			}

			return node;
		}
	}
}
