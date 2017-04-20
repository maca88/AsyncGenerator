using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		private MethodTransformationResult TransformMethod(TypeTransformationMetadata typeMetadata, IMethodAnalyzationResult methodResult)
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

			foreach (var pair in referenceAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(pair.Key).OfType<SimpleNameSyntax>().First();
				var funReferenceResult = pair.Value;
				var invokeFuncReferenceResult = funReferenceResult as IInvokeFunctionReferenceAnalyzationResult;
				// If we have a cref just change the name to the async counterpart
				if (invokeFuncReferenceResult == null)
				{
					methodNode = methodNode
						.ReplaceNode(nameNode, nameNode
							.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
							.WithTriviaFrom(nameNode));
					continue;
				}
				if (!invokeFuncReferenceResult.AwaitInvocation)
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
						var newStatement = statement.ReplaceNode(nameNode, newNameNode);
						if (invokeFuncReferenceResult?.UseAsReturnValue == true)
						{
							newStatement = newStatement.ToReturnStatement();
						}
						methodNode = methodNode
							.ReplaceNode(statement, newStatement);
					}
				}

			}

			if (methodResult.OmitAsync)
			{
				// TODO: wrap in a task when calling non taskable method or throwing an exception in a non precondition statement
			}
			methodNode = methodNode.ReturnAsTask(methodResult.Symbol)
				.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
			result.TransformedNode = methodNode;

			return result;
		}
	}
}
