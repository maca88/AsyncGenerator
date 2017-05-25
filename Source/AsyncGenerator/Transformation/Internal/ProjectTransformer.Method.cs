using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
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
		private MethodTransformationResult TransformMethod(MethodDeclarationSyntax methodNode, bool canCopy, MethodTransformationResult result, ITypeTransformationMetadata typeMetadata, 
			INamespaceTransformationMetadata namespaceMetadata)
		{
			//var result = new MethodTransformationResult(methodResult);
			var methodResult = result.AnalyzationResult;
			var methodConversion = methodResult.Conversion;
			if (!canCopy)
			{
				methodConversion &= ~MethodConversion.Copy;
			}
			//var methodNode = customNode ?? methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();

			// Calculate whitespace method trivias
			result.EndOfLineTrivia = methodNode.GetEndOfLine();
			result.LeadingWhitespaceTrivia = methodNode.GetLeadingWhitespace();
			result.IndentTrivia = methodNode.GetIndent(result.LeadingWhitespaceTrivia, typeMetadata.LeadingWhitespaceTrivia);
			result.BodyLeadingWhitespaceTrivia = Whitespace(result.LeadingWhitespaceTrivia.ToFullString() + result.IndentTrivia.ToFullString());

			if (methodConversion == MethodConversion.Ignore)
			{
				return result;
			}

			if (methodBodyNode == null)
			{
				if (methodConversion.HasFlag(MethodConversion.ToAsync))
				{
					result.Transformed = methodNode.ReturnAsTask(namespaceMetadata.TaskConflict)
						.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
					if (methodConversion.HasFlag(MethodConversion.Copy))
					{
						result.AddMethod(methodResult.Node);
					}
					return result;
				}
				if (methodConversion.HasFlag(MethodConversion.Copy))
				{
					result.Transformed = methodResult.Node;
				}
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;

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

			// For copied methods we need just to replace type references
			if (methodConversion.HasFlag(MethodConversion.Copy))
			{
				var copiedMethod = methodNode;
				// Modify references
				foreach (var refAnnotation in typeReferencesAnnotations)
				{
					var nameNode = copiedMethod.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
					copiedMethod = copiedMethod
						.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
				}
				if (!methodConversion.HasFlag(MethodConversion.ToAsync))
				{
					result.Transformed = copiedMethod;
					return result;
				}
				result.AddMethod(copiedMethod.WithoutAnnotations(result.Annotation));
			}

			foreach (var childFunction in methodResult.ChildFunctions.Where(o => o.Conversion != MethodConversion.Ignore))
			{
				var functionNode = childFunction.GetNode();
				var functionKind = functionNode.Kind();
				var typeSpanStart = functionNode.SpanStart - startMethodSpan;
				var typeSpanLength = functionNode.Span.Length;
				var funcNode = methodNode.DescendantNodesAndSelf()
					.First(o => o.IsKind(functionKind) && o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				var transformFuncResult = TransformFunction(childFunction, typeMetadata, namespaceMetadata);
				result.TransformedFunctions.Add(transformFuncResult);
				methodNode = methodNode.ReplaceNode(funcNode, funcNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformFuncResult.Annotation)));
			}

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

			foreach (var transformFunction in result.TransformedFunctions)
			{
				var funcNode = methodNode.GetAnnotatedNodes(transformFunction.Annotation).First();
				methodNode = methodNode
					.ReplaceNode(funcNode, transformFunction.Transformed);
			}

			// We have to order by OriginalStartSpan in order to have consistent formatting when adding awaits
			foreach (var transfromReference in result.TransformedFunctionReferences.OrderByDescending(o => o.OriginalStartSpan))
			{
				methodNode = TransformFunctionReference(methodNode, methodResult, transfromReference, namespaceMetadata);
			}

			if (methodResult.RewriteYields)
			{
				var yieldRewriter = new YieldRewriter(result);
				methodNode = (MethodDeclarationSyntax)yieldRewriter.VisitMethodDeclaration(methodNode);
			}

			if (!methodResult.SplitTail && !methodResult.PreserveReturnType && methodResult.OmitAsync)
			{
				var rewriter = new ReturnTaskMethodRewriter(result, namespaceMetadata);
				methodNode = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodNode);
			}
			else if(!methodResult.OmitAsync)
			{
				methodNode = methodNode.AddAsync();
			}

			methodNode = methodNode
				.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
			if (!methodResult.PreserveReturnType)
			{
				methodNode = methodNode.ReturnAsTask(namespaceMetadata.TaskConflict);
			}
			result.Transformed = methodNode;

			return result;
		}
	}
}
