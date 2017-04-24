using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		// TODO: Missing members
		private TypeTransformationResult TransformType(ITypeAnalyzationResult rootTypeResult)
		{
			var rootTypeNode = rootTypeResult.Node;
			var startRootTypeSpan = rootTypeNode.SpanStart;
			var typeResultMetadatas = new Dictionary<ITypeAnalyzationResult, TypeTransformationMetadata>();
			var rootMetadata = new TypeTransformationMetadata
			{
				ReservedFieldNames = new HashSet<string>(rootTypeResult.Symbol.MemberNames)
			};
			// We do this here because we want that the root node has span start equal to 0
			rootTypeNode = rootTypeNode.WithAdditionalAnnotations(new SyntaxAnnotation(rootMetadata.Annotation));
			startRootTypeSpan -= rootTypeNode.SpanStart;

			// Before any modification we need to annotate nodes that will be transformed in order to find them later on. 
			// We cannot rely on spans as they changes each time the node is modified.
			foreach (var typeResult in rootTypeResult.GetSelfAndDescendantsTypes(o => o.Conversion != TypeConversion.Ignore))
			{
				var typeSpanStart = typeResult.Node.SpanStart - startRootTypeSpan;
				var typeSpanLength = typeResult.Node.Span.Length;
				var typeNode = rootTypeNode.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
					.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				var leadingWhitespaceTrivia = typeNode.GetFirstToken().LeadingTrivia.First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));
				TypeTransformationMetadata metadata;
				if (typeNode == rootTypeNode)
				{
					metadata = rootMetadata;
				}
				else
				{
					metadata = new TypeTransformationMetadata
					{
						ReservedFieldNames = new HashSet<string>(typeResult.Symbol.MemberNames)
					};
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(metadata.Annotation)));
				}
				metadata.LeadingWhitespaceTrivia = leadingWhitespaceTrivia;

				// TypeReferences can be changes only if we create a new type
				if (rootTypeResult.Conversion == TypeConversion.NewType)
				{
					foreach (var typeReference in typeResult.TypeReferences)
					{
						var reference = typeReference.ReferenceLocation;
						var refSpanStart = reference.Location.SourceSpan.Start - startRootTypeSpan;
						var refSpanLength = reference.Location.SourceSpan.Length;
						if (refSpanStart < 0)
						{
							// TODO: cref
							//var startSpan = reference.Location.SourceSpan.Start - rootTypeInfo.Node.GetLeadingTrivia().Span.Start;
							//var crefNode = leadingTrivia.First(o => o.SpanStart == startSpan && o.Span.Length == refSpanLength);
							continue;
						}

						var nameNode = rootTypeNode.GetSimpleName(refSpanStart, refSpanLength);
						var transformedNode = new TransformationResult(nameNode)
						{
							TransformedNode = nameNode.WithIdentifier(Identifier(nameNode.Identifier.ValueText + "Async"))
						};
						metadata.TransformedNodes.Add(transformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));

					}
				}

				// Annotate and save the transformed methods
				foreach (var methodResult in typeResult.Methods)
				{
					var methodSpanStart = methodResult.Node.SpanStart - startRootTypeSpan;
					var methodSpanLength = methodResult.Node.Span.Length;
					var methodNode = rootTypeNode.DescendantNodes()
											 .OfType<MethodDeclarationSyntax>()
											 .First(o => o.SpanStart == methodSpanStart && o.Span.Length == methodSpanLength);
					var transformedNode = TransformMethod(metadata, methodResult);
					metadata.TransformedMethods.Add(transformedNode);
					rootTypeNode = rootTypeNode.ReplaceNode(methodNode, methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));

				}
				typeResultMetadatas.Add(typeResult, metadata);

			}
			// Save the orignal node that was only annotated
			var result = new TypeTransformationResult(rootTypeNode);

			// Now we can start transforming the type. Start from the bottom in order to preserve replaced nested types
			foreach (var typeResult in rootTypeResult.GetSelfAndDescendantsTypes(o => o.Conversion != TypeConversion.Ignore)
				.OrderByDescending(o => o.Node.SpanStart))
			{
				var metadata = typeResultMetadatas[typeResult];
				// Add partial keyword on the original node if not present
				if (typeResult.Conversion == TypeConversion.Partial && !typeResult.IsPartial)
				{
					var typeNode = rootTypeNode.GetAnnotatedNodes(metadata.Annotation).OfType<TypeDeclarationSyntax>().First();
					result.OriginalModifiedNode = result.Node.ReplaceNode(typeNode, typeNode.AddPartial());
				}
				// If the root type has to be a new type then all nested types have to be new types
				if (typeResult.Conversion == TypeConversion.NewType)
				{
					// Replace all rewritten nodes
					foreach (var rewNode in metadata.TransformedNodes)
					{
						var node = rootTypeNode.GetAnnotatedNodes(rewNode.Annotation).First();
						if (rewNode.TransformedNode == null)
						{
							//TODO: fix regions
							rootTypeNode = rootTypeNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
						}
						else
						{
							rootTypeNode = rootTypeNode.ReplaceNode(node, rewNode.TransformedNode);
						}
					}
				}
				else if (typeResult.Conversion == TypeConversion.Partial)
				{
					var typeNode = rootTypeNode.GetAnnotatedNodes(metadata.Annotation).OfType<TypeDeclarationSyntax>().First();
					var newNodes = metadata.TransformedNodes
							.Union(metadata.TransformedMethods)
							.Where(o => o.TransformedNode != null)
							.OrderBy(o => o.Node.SpanStart)
							.Select(o => o.TransformedNode)
						.Union(typeNode.DescendantNodes().OfType<TypeDeclarationSyntax>())
						.ToList();
					if (!newNodes.Any())
					{
						//TODO: fix regions
						rootTypeNode = rootTypeNode.RemoveNode(typeNode, SyntaxRemoveOptions.KeepNoTrivia);
					}
					else
					{
						// We need to remove the attributes as they cannot be defined in both partial classes
						var newTypeNode = typeNode.AddPartial().WithoutAttributes();
						// Add fields for async lock if any. We need a lock field for each synchronized method
						foreach (var methodTransform in metadata.TransformedMethods.Where(o => o.AsyncLockField != null).OrderBy(o => o.Node.SpanStart))
						{
							newNodes.Insert(0, methodTransform.AsyncLockField);
						}
						newTypeNode = newTypeNode.WithMembers(List(newNodes));

						//TODO: fix regions
						rootTypeNode = rootTypeNode.ReplaceNode(typeNode, newTypeNode/*.RemoveLeadingDirectives()*/);
					}
				}
			}

			result.TransformedNode = rootTypeNode;

			return result;
		}


	}
}
