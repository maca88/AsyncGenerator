using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private MethodTransformationResult TransformMethod(MethodTransformationResult result, ITypeTransformationMetadata typeMetadata, MethodDeclarationSyntax customNode = null)
		{
			//var result = new MethodTransformationResult(methodResult);
			var methodResult = result.AnalyzationResult;
			var methodNode = customNode ?? methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();
			var cancellationTokenParamName = "cancellationToken"; // TODO: remove

			// Calculate whitespace method trivias
			result.EndOfLineTrivia = methodNode.GetEndOfLine();
			result.LeadingWhitespaceTrivia = methodNode.GetLeadingWhitespace();
			result.IndentTrivia = methodNode.GetIndent(result.LeadingWhitespaceTrivia, typeMetadata.LeadingWhitespaceTrivia);
			result.BodyLeadingWhitespaceTrivia = Whitespace(result.LeadingWhitespaceTrivia.ToFullString() + result.IndentTrivia.ToFullString());

			if (methodBodyNode == null)
			{
				result.Transformed = methodNode.ReturnAsTask()
					.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;

			if (methodResult.Conversion == MethodConversion.Ignore)
			{
				return result;
			}

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in methodResult.TypeReferences)
			{
				var reference = typeReference.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			//var referenceAnnotations = new Dictionary<string, IFunctionReferenceAnalyzationResult>();
			foreach (var referenceResult in methodResult.CrefMethodReferences
				.Union(methodResult.MethodReferences)
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var transfromReference = new FunctionReferenceTransformationResult(referenceResult);
				var isCref = referenceResult is CrefFunctionReferenceData;
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length, isCref);
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transfromReference.Annotation)));
				result.TransformedFunctionReferences.Add(transfromReference);

				if (isCref || !methodResult.OmitAsync)
				{
					continue;
				}
				// We need to annotate the reference node (InvocationExpression, IdentifierName) in order to know if we need to wrap the node in a Task.FromResult
				var refNode = referenceResult.ReferenceNode;
				var bodyReference = (IBodyFunctionReferenceAnalyzationResult) referenceResult;
				if (bodyReference.UseAsReturnValue || refNode.IsReturned())
				{
					startSpan = refNode.SpanStart - startMethodSpan;
					var referenceNode = methodNode.DescendantNodes().First(o => o.SpanStart == startSpan && o.Span.Length == refNode.Span.Length);
					methodNode = methodNode.ReplaceNode(referenceNode, referenceNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.TaskReturnedAnnotation)));
				}
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				methodNode = methodNode
							.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
			}

			foreach (var transfromReference in result.TransformedFunctionReferences)
			{
				var nameNode = methodNode.GetAnnotatedNodes(transfromReference.Annotation).OfType<SimpleNameSyntax>().First();
				var funReferenceResult = transfromReference.AnalyzationResult;
				var bodyFuncReferenceResult = funReferenceResult as IBodyFunctionReferenceAnalyzationResult;
				var newNameNode = nameNode
					.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
					.WithTriviaFrom(nameNode);
				transfromReference.Transformed = newNameNode;
				// If we have a cref change the name to the async counterpart and add/update arguments
				if (bodyFuncReferenceResult == null)
				{
					var crefNode = (NameMemberCrefSyntax) nameNode.Parent;
					var paramList = new List<CrefParameterSyntax>();
					// If the cref has already the parameters set then use them
					if (crefNode.Parameters != null)
					{
						paramList.AddRange(crefNode.Parameters.Parameters);
					}
					else
					{
						// We have to add the parameter to avoid ambiguity
						var asyncSymbol = funReferenceResult.AsyncCounterpartSymbol;
						//TODO: take care of type namespaces (do not include the full type name if is not required)
						paramList.AddRange(asyncSymbol.Parameters
							.Select(o => CrefParameter(o.Type
								.CreateTypeSyntax(true, methodResult.CancellationTokenRequired && o.Type.ContainingNamespace?.ToString() == "System.Threading"))));
					}
					
					// If the async counterpart is internal and a token is required add a token parameter
					if (funReferenceResult.AsyncCounterpartFunction?.GetMethod()?.CancellationTokenRequired == true)
					{
						paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
					}
					methodNode = methodNode
						.ReplaceNode(crefNode, crefNode
								.ReplaceNode(nameNode, newNameNode)
								.WithParameters(CrefParameterList(SeparatedList(paramList))))
						;
					continue;
				}

				InvocationExpressionSyntax invokeNode = null;
				if (bodyFuncReferenceResult.CancellationTokenRequired || bodyFuncReferenceResult.AwaitInvocation)
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
							methodNode = methodNode.ReplaceNode(invokeNode, invokeNode
								.ReplaceNode(nameNode, newNameNode)
								.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired));
						}
						else
						{
							methodNode = methodNode
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
								.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired));
						}
						else
						{
							newStatement = statement.ReplaceNode(nameNode, newNameNode);
						}

						if (bodyFuncReferenceResult.UseAsReturnValue)
						{
							newStatement = newStatement.ToReturnStatement();
						}

						methodNode = methodNode
							.ReplaceNode(statement, newStatement);
					}
				}
				else
				{
					// We need to annotate the invocation node because of the AddAwait method as it needs the parent node
					var invokeAnnotation = Guid.NewGuid().ToString();
					methodNode = methodNode
						.ReplaceNode(invokeNode, invokeNode
						.ReplaceNode(nameNode, newNameNode)
						.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired)
						.WithAdditionalAnnotations(new SyntaxAnnotation(invokeAnnotation))
					);
					invokeNode = methodNode.GetAnnotatedNodes(invokeAnnotation).OfType<InvocationExpressionSyntax>().First();
					methodNode = methodNode.ReplaceNode(invokeNode, invokeNode.AddAwait(_configuration.ConfigureAwaitArgument));
				}

			}

			if (methodResult.RewriteYields)
			{
				var yieldRewriter = new YieldRewriter(result);
				methodNode = (MethodDeclarationSyntax)yieldRewriter.VisitMethodDeclaration(methodNode);
			}

			if (!methodResult.SplitTail && methodResult.OmitAsync)
			{
				var rewriter = new ReturnTaskMethodRewriter(result);
				methodNode = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodNode);
			}
			else if(!methodResult.SplitTail)
			{
				methodNode = methodNode.AddAsync();
			}

			methodNode = methodNode.ReturnAsTask()
				.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
			result.Transformed = methodNode;

			return result;
		}
	}
}
