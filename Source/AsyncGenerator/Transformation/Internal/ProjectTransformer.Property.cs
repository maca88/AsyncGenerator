using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
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
		private PropertyTransformationResult TransformProperty(PropertyDeclarationSyntax propertyNode, bool canCopy, PropertyTransformationResult result, ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			var analyzeResult = result.AnalyzationResult;
			var startRootSpan = analyzeResult.Node.SpanStart;

			// Calculate whitespace method trivias
			result.EndOfLineTrivia = propertyNode.GetEndOfLine();
			result.LeadingWhitespaceTrivia = propertyNode.GetLeadingWhitespace();
			result.IndentTrivia = propertyNode.GetIndent(result.LeadingWhitespaceTrivia, typeMetadata.LeadingWhitespaceTrivia);
			result.BodyLeadingWhitespaceTrivia = Whitespace(result.LeadingWhitespaceTrivia.ToFullString() + result.IndentTrivia.ToFullString());

			if (analyzeResult.Conversion == PropertyConversion.Ignore && analyzeResult.GetAccessors().All(o => o.Conversion == MethodConversion.Ignore))
			{
				return result;
			}

			propertyNode = propertyNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startRootSpan -= propertyNode.SpanStart;

			foreach (var accessorResult in analyzeResult.GetAccessors().Where(o => o.Conversion != MethodConversion.Ignore))
			{
				var spanStart = accessorResult.Node.SpanStart - startRootSpan;
				var spanLength = accessorResult.Node.Span.Length;
				var accessorNode = propertyNode.DescendantNodes()
					.First(o => o.SpanStart == spanStart && o.Span.Length == spanLength);
				var transformedNode = new AccessorTransformationResult(accessorResult);
				result.TransformedAccessors.Add(transformedNode);
				propertyNode = propertyNode.ReplaceNode(accessorNode, accessorNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
			}

			if (canCopy && result.AnalyzationResult.Conversion == PropertyConversion.Copy)
			{
				result.Transformed = result.OriginalNode;
			}

			foreach (var accessorResult in result.TransformedAccessors.OrderByDescending(o => o.OriginalStartSpan))
			{
				var accessorNode = propertyNode.GetAnnotatedNodes(accessorResult.Annotation)
					.First();
				TransformPropertyAccessor(accessorNode, result, accessorResult, typeMetadata, namespaceMetadata);
			}

			return result;
		}

		private void TransformPropertyAccessor(SyntaxNode node, PropertyTransformationResult propertyResult, AccessorTransformationResult result, ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = result.AnalyzationResult;
			result.BodyLeadingWhitespaceTrivia = propertyResult.BodyLeadingWhitespaceTrivia;
			result.LeadingWhitespaceTrivia = propertyResult.LeadingWhitespaceTrivia;
			result.EndOfLineTrivia = propertyResult.EndOfLineTrivia;
			result.IndentTrivia = propertyResult.IndentTrivia;

			var methodConversion = methodResult.Conversion;
			if (!methodConversion.HasFlag(MethodConversion.ToAsync))
			{
				return;
			}
			MethodDeclarationSyntax methodNode;
			var methodBodyNode = methodResult.GetBodyNode();
			if (methodBodyNode == null)
			{
				methodNode = MethodDeclaration(
						propertyResult.OriginalNode.Type,
						methodResult.AsyncCounterpartName
					)
					.WithModifiers(propertyResult.OriginalNode.Modifiers)
					.WithLeadingTrivia(propertyResult.OriginalNode.GetLeadingTrivia())
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(propertyResult.EndOfLineTrivia)));
				if (!methodResult.PreserveReturnType)
				{
					methodNode = methodNode.ReturnAsTask(namespaceMetadata.TaskConflict);
				}
				result.Transformed = methodNode;
				return;
			}

			var startMethodSpan = methodResult.Node.Span.Start;
			node = node.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= node.SpanStart;

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in methodResult.TypeReferences)
			{
				var reference = typeReference.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = node.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				node = node.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			foreach (var childFunction in methodResult.ChildFunctions.Where(o => o.Conversion != MethodConversion.Ignore))
			{
				var functionNode = childFunction.GetNode();
				var functionKind = functionNode.Kind();
				var typeSpanStart = functionNode.SpanStart - startMethodSpan;
				var typeSpanLength = functionNode.Span.Length;
				var funcNode = node.DescendantNodesAndSelf()
					.First(o => o.IsKind(functionKind) && o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				var transformFuncResult = TransformFunction(childFunction, typeMetadata, namespaceMetadata);
				result.TransformedFunctions.Add(transformFuncResult);
				node = node.ReplaceNode(funcNode, funcNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformFuncResult.Annotation)));
			}


			foreach (var referenceResult in methodResult.CrefMethodReferences
				.Union(methodResult.MethodReferences)
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var transfromReference = new FunctionReferenceTransformationResult(referenceResult);
				var isCref = referenceResult is CrefFunctionReferenceData;
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = node.GetSimpleName(startSpan, reference.Location.SourceSpan.Length, isCref);
				node = node.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transfromReference.Annotation)));
				result.TransformedFunctionReferences.Add(transfromReference);

				if (isCref || !methodResult.OmitAsync)
				{
					continue;
				}
				// We need to annotate the reference node (InvocationExpression, IdentifierName) in order to know if we need to wrap the node in a Task.FromResult
				var refNode = referenceResult.ReferenceNode;
				var bodyReference = (IBodyFunctionReferenceAnalyzationResult)referenceResult;
				if (bodyReference.UseAsReturnValue || refNode.IsReturned())
				{
					startSpan = refNode.SpanStart - startMethodSpan;
					var referenceNode = node.DescendantNodes().First(o => o.SpanStart == startSpan && o.Span.Length == refNode.Span.Length);
					node = node.ReplaceNode(referenceNode, referenceNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.TaskReturnedAnnotation)));
				}
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = node.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				node = node
					.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
			}

			foreach (var transformFunction in result.TransformedFunctions)
			{
				var funcNode = node.GetAnnotatedNodes(transformFunction.Annotation).First();
				node = node
					.ReplaceNode(funcNode, transformFunction.Transformed);
			}

			// We have to order by OriginalStartSpan in order to have consistent formatting when adding awaits
			foreach (var transfromReference in result.TransformedFunctionReferences.OrderByDescending(o => o.OriginalStartSpan))
			{
				node = TransformFunctionReference(node, methodResult, transfromReference, namespaceMetadata);
			}

			methodNode = MethodDeclaration(
					propertyResult.OriginalNode.Type,
					methodResult.AsyncCounterpartName
				)
				.WithModifiers(propertyResult.OriginalNode.Modifiers)
				.WithLeadingTrivia(propertyResult.OriginalNode.GetLeadingTrivia());
			if (node is ArrowExpressionClauseSyntax arrowNode)
			{
				methodNode = methodNode
					.WithExpressionBody(arrowNode)
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(propertyResult.EndOfLineTrivia)));
			}
			else if (node is AccessorDeclarationSyntax accessorNode)
			{
				if (accessorNode.ExpressionBody != null)
				{
					methodNode = methodNode
						.WithExpressionBody(accessorNode.ExpressionBody)
						.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(propertyResult.EndOfLineTrivia)));
				}
				else
				{
					methodNode = methodNode
						.WithBody(accessorNode.Body);
				}
				
			}
			methodNode = FixupBodyFormatting(methodNode, result);

			// TODO: to plugins
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
			else if (!methodResult.OmitAsync)
			{
				methodNode = methodNode.AddAsync();
			}
			if (!methodResult.PreserveReturnType)
			{
				methodNode = methodNode.ReturnAsTask(namespaceMetadata.TaskConflict);
			}
			result.Transformed = methodNode;
		}
	}
}
