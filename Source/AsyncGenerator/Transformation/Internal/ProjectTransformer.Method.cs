using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using  static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private MethodTransformationResult TransformMethod(TypeTransformationMetadata typeMetadata, 
			IMethodAnalyzationResult methodResult)
		{
			var result = new MethodTransformationResult(methodResult);
			if (methodResult.Conversion == MethodConversion.Ignore)
			{
				return result;
			}
			var methodNode = methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();
			if (methodBodyNode == null)
			{
				result.TransformedNode = methodNode.ReturnAsTask(methodResult.Symbol)
					.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;
			// TODO: get leading trivia for the method
			var endOfLineTrivia = methodNode.DescendantTrivia().First(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
			var methodLeadingTrivia = methodNode.GetFirstToken().LeadingTrivia.First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));

			var bodyWhitespaceTrivia = methodLeadingTrivia.ToFullString() + methodLeadingTrivia.ToFullString()
					.Substring(typeMetadata.LeadingWhitespaceTrivia.ToFullString().Length);


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

			var referenceAnnotations = new Dictionary<string, IFunctionReferenceAnalyzationResult>();
			foreach (var referenceResult in methodResult.CrefMethodReferences
				.Union(methodResult.MethodReferences)
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length, referenceResult is CrefFunctionReferenceData);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				referenceAnnotations.Add(annotation, referenceResult);
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				methodNode = methodNode
							.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
			}
			
			var cancellationTokenParamName = "cancellationToken";
			// TODO: handle variable collision for token
			methodNode = methodNode.AddCancellationTokenParameterIf(cancellationTokenParamName, methodResult.CancellationTokenRequired);

			foreach (var pair in referenceAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(pair.Key).OfType<SimpleNameSyntax>().First();
				var funReferenceResult = pair.Value;
				var bodyFuncReferenceResult = funReferenceResult as IBodyFunctionReferenceAnalyzationResult;
				// If we have a cref just change the name to the async counterpart (TODO: add the arguments as there may be multiple methods with the same name)
				if (bodyFuncReferenceResult == null)
				{
					var crefNode = (NameMemberCrefSyntax) nameNode.Parent;
					var paramList = new List<CrefParameterSyntax>();
					var asyncSymbol = funReferenceResult.AsyncCounterpartSymbol;
					//TODO: take care of type namespaces
					paramList.AddRange(asyncSymbol.Parameters.Select(o => CrefParameter(IdentifierName(o.Type.Name))));
					// If the async counterpart is internal and a token is required add a token parameter
					if (funReferenceResult.AsyncCounterpartFunction?.GetMethod()?.CancellationTokenRequired == true)
					{
						paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
					}
					methodNode = methodNode
						.ReplaceNode(crefNode, crefNode
								.ReplaceNode(nameNode, nameNode
									.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
									.WithTriviaFrom(nameNode))
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
					//TODO: arrow method
					var statement = nameNode.Ancestors().OfType<StatementSyntax>().FirstOrDefault();

					var newNameNode = nameNode
						.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
						.WithTriviaFrom(nameNode);
					// An arrow method will not have a statement
					if (statement == null)
					{
						methodNode = methodNode
							.ReplaceNode(nameNode, newNameNode);
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
					var newNameNode = nameNode
						.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
						.WithTriviaFrom(nameNode);
					var invokeParent = invokeNode.Parent;
					methodNode = methodNode.ReplaceNode(invokeNode, invokeNode
						.ReplaceNode(nameNode, newNameNode)
						.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired)
						.AddAwait(invokeParent, _configuration.ConfigureAwaitArgument)
					);
				}

			}

			if (methodResult.OmitAsync)
			{
				if (methodResult.MethodReferences.All(o => o.GetConversion() == ReferenceConversion.Ignore))
				{
					
				}

				// TODO: wrap in a task when calling non taskable method or throwing an exception in a non precondition statement
			}
			else
			{
				methodNode = methodNode.AddAsync();
			}
			methodNode = methodNode.ReturnAsTask(methodResult.Symbol)
				.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
			result.TransformedNode = methodNode;

			return result;
		}
	}
}
