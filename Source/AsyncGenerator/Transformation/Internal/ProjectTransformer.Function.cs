using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{

		private RootFunctionTransformationResult TransformFunction(IFunctionAnalyzationResult rootFuncResult, 
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var rootFuncNode = rootFuncResult.GetNode();
			var startRootFuncSpan = rootFuncNode.SpanStart;
			var rootTransformResult = new RootFunctionTransformationResult(rootFuncResult)
			{
			};
			// We do this here because we want that the root node has span start equal to 0
			rootFuncNode = rootFuncNode.WithAdditionalAnnotations(new SyntaxAnnotation(rootTransformResult.Annotation));
			startRootFuncSpan -= rootFuncNode.SpanStart;

			// Before any modification we need to annotate nodes that will be transformed in order to find them later on. 
			foreach (var funcResult in rootFuncResult.GetSelfAndDescendantsFunctions())
			{
				var origFuncNode = funcResult.GetNode();
				var funcSpanStart = origFuncNode.SpanStart - startRootFuncSpan;
				var funcSpanLength = origFuncNode.Span.Length;
				var funcNode = rootFuncNode.DescendantNodesAndSelf()
					.First(o => o.SpanStart == funcSpanStart && o.Span.Length == funcSpanLength);
				var leadingWhitespace = funcNode.GetLeadingWhitespace();
				FunctionTransformationResult transformResult;
				if (funcNode == rootFuncNode)
				{
					transformResult = rootTransformResult;
					//transformResult.IndentTrivia = typeNode.GetIndent(leadingWhitespace, namespaceMetadata.LeadingWhitespaceTrivia);
				}
				else
				{
					transformResult = new FunctionTransformationResult(funcResult)
					{
						//IndentTrivia = typeNode.GetIndent(leadingWhitespace)
					};
					rootFuncNode = rootFuncNode.ReplaceNode(funcNode, funcNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformResult.Annotation)));
					rootTransformResult.DescendantTransformedFunctions.Add(transformResult);
				}
				if (funcResult.Conversion == MethodConversion.Ignore)
				{
					continue; // The ignored type shall be only annotated
				}

				// TODO: unify with method in order to avoid duplicate code
				foreach (var referenceResult in funcResult.MethodReferences.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
				{
					var transfromReference = new FunctionReferenceTransformationResult(referenceResult);
					var reference = referenceResult.ReferenceLocation;
					var startSpan = reference.Location.SourceSpan.Start - startRootFuncSpan;
					var nameNode = rootFuncNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
					rootFuncNode = rootFuncNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transfromReference.Annotation)));
					transformResult.TransformedFunctionReferences.Add(transfromReference);

					if (!funcResult.OmitAsync)
					{
						continue;
					}
					// We need to annotate the reference node (InvocationExpression, IdentifierName) in order to know if we need to wrap the node in a Task.FromResult
					var refNode = referenceResult.ReferenceNode;
					if (referenceResult.UseAsReturnValue || refNode.IsReturned())
					{
						startSpan = refNode.SpanStart - startRootFuncSpan;
						var referenceNode = rootFuncNode.DescendantNodes().First(o => o.SpanStart == startSpan && o.Span.Length == refNode.Span.Length);
						rootFuncNode = rootFuncNode.ReplaceNode(referenceNode, referenceNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformResult.TaskReturnedAnnotation)));
					}
				}
				foreach (var typeReference in funcResult.TypeReferences)
				{
					var reference = typeReference.ReferenceLocation;
					var refSpanStart = reference.Location.SourceSpan.Start - startRootFuncSpan;
					var refSpanLength = reference.Location.SourceSpan.Length;
					var nameNode = rootFuncNode.GetSimpleName(refSpanStart, refSpanLength, typeReference.IsCref);
					var transformedNode = new TransformationResult(nameNode)
					{
						Transformed = nameNode.WithIdentifier(Identifier(nameNode.Identifier.ValueText + "Async").WithTriviaFrom(nameNode.Identifier))
					};
					transformResult.TransformedNodes.Add(transformedNode);
					rootFuncNode = rootFuncNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
				}

			}

			foreach (var transformResult in rootTransformResult.GetSelfAndDescendantTransformedFunctions().OrderByDescending(o => o.OriginalStartSpan))
			{
				// We have to order by OriginalStartSpan in order to have consistent formatting when adding awaits
				foreach (var transfromReference in transformResult.TransformedFunctionReferences.OrderByDescending(o => o.OriginalStartSpan))
				{
					rootFuncNode = TransformFunctionReference(rootFuncNode, rootFuncResult, transfromReference, namespaceMetadata);
				}

				// Replace all rewritten nodes
				foreach (var rewNode in transformResult.TransformedNodes)
				{
					var node = rootFuncNode.GetAnnotatedNodes(rewNode.Annotation).First();
					if (rewNode.Transformed == null)
					{
						rootFuncNode = rootFuncNode.RemoveNode(node, SyntaxRemoveOptions.KeepUnbalancedDirectives);
					}
					else
					{
						rootFuncNode = rootFuncNode.ReplaceNode(node, rewNode.Transformed);
					}
				}

				var funcNode = rootFuncNode.GetAnnotatedNodes(transformResult.Annotation).First();
				var newFuncNode = funcNode;
				if (!transformResult.AnalyzationResult.OmitAsync)
				{
					newFuncNode = newFuncNode.AddAsync();
				}
				transformResult.Transformed = newFuncNode;
				rootFuncNode = rootFuncNode.ReplaceNode(funcNode, newFuncNode);
			}

			rootTransformResult.Transformed = rootFuncNode;
			return rootTransformResult;
		}

	}
}
