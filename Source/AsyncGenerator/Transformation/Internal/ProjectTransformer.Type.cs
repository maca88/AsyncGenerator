using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		// TODO: Missing members
		private RootTypeTransformationResult TransformType(ITypeAnalyzationResult rootTypeResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			var rootTypeNode = rootTypeResult.Node;
			var startRootTypeSpan = rootTypeNode.SpanStart;
			var rootTransformResult = new RootTypeTransformationResult(rootTypeResult)
			{
				MemberNames = rootTypeResult.Symbol.MemberNames.ToImmutableHashSet(),

			};
			// We do this here because we want that the root node has span start equal to 0
			rootTypeNode = rootTypeNode.WithAdditionalAnnotations(new SyntaxAnnotation(rootTransformResult.Annotation));
			startRootTypeSpan -= rootTypeNode.SpanStart;

			// Before any modification we need to annotate nodes that will be transformed in order to find them later on. 
			// We cannot rely on spans as they changes each time the node is modified.
			// We need to annotate also the ignored types to be later removed
			foreach (var typeResult in rootTypeResult.GetSelfAndDescendantsTypes())
			{
				var typeSpanStart = typeResult.Node.SpanStart - startRootTypeSpan;
				var typeSpanLength = typeResult.Node.Span.Length;
				var typeNode = rootTypeNode.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
					.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				var leadingWhitespace = typeNode.GetLeadingWhitespace();
				TypeTransformationResult transformResult;
				if (typeNode == rootTypeNode)
				{
					transformResult = rootTransformResult;
					transformResult.IndentTrivia = typeNode.GetIndent(leadingWhitespace, namespaceMetadata.LeadingWhitespaceTrivia);
				}
				else
				{
					transformResult = new TypeTransformationResult(typeResult)
					{
						MemberNames = typeResult.Symbol.MemberNames.ToImmutableHashSet(),
						IndentTrivia = typeNode.GetIndent(leadingWhitespace)
					};
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformResult.Annotation)));
					rootTransformResult.DescendantTransformedTypes.Add(transformResult);
				}
				transformResult.LeadingWhitespaceTrivia = leadingWhitespace;
				transformResult.EndOfLineTrivia = typeNode.GetEndOfLine();

				if (typeResult.Conversion == TypeConversion.Ignore)
				{
					continue; // The ignored type shall be only annotated
				}

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
							Transformed = nameNode.WithIdentifier(Identifier(nameNode.Identifier.ValueText + "Async"))
						};
						transformResult.TransformedNodes.Add(transformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));

					}
				}

				// Annotate and save an empty method transformation result
				foreach (var methodResult in typeResult.Methods/*.Where(o => o.Conversion != MethodConversion.Ignore)*/)
				{
					var methodSpanStart = methodResult.Node.SpanStart - startRootTypeSpan;
					var methodSpanLength = methodResult.Node.Span.Length;
					var methodNode = rootTypeNode.DescendantNodes()
						.OfType<MethodDeclarationSyntax>()
						.First(o => o.SpanStart == methodSpanStart && o.Span.Length == methodSpanLength);
					// Only create the transformation result for the method and transform the method later as the method may change
					// (a directive may be added to the method when removing type members)
					var transformedNode = new MethodTransformationResult(methodResult);
					transformResult.TransformedMethods.Add(transformedNode);
					rootTypeNode = rootTypeNode.ReplaceNode(methodNode, methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
				}
			}
			// Save the orignal node that was only annotated
			var originalAnnotatedNode = rootTypeNode;

			// Now we can start transforming the type. Start from the bottom in order to preserve replaced nested types
			foreach (var transformResult in rootTransformResult.GetSelfAndDescendantTransformedTypes().OrderByDescending(o => o.OriginalNode.SpanStart))
			{
				var typeResult = transformResult.AnalyzationResult;
				

				// Add partial keyword on the original node if not present
				if (typeResult.Conversion == TypeConversion.Partial && !typeResult.IsPartial)
				{
					if (rootTransformResult.OriginalModified == null)
					{
						rootTransformResult.OriginalModified = originalAnnotatedNode;
					}
					var typeNode = rootTransformResult.OriginalModified.GetAnnotatedNodes(transformResult.Annotation).OfType<TypeDeclarationSyntax>().First();
					rootTransformResult.OriginalModified = rootTransformResult.OriginalModified.ReplaceNode(typeNode, typeNode.AddPartial());
				}
				// If the root type has to be a new type then all nested types have to be new types
				if (typeResult.Conversion == TypeConversion.NewType)
				{
					// Replace all rewritten nodes
					foreach (var rewNode in transformResult.TransformedNodes)
					{
						var node = rootTypeNode.GetAnnotatedNodes(rewNode.Annotation).First();
						if (rewNode.Transformed == null)
						{
							//TODO: fix regions
							rootTypeNode = rootTypeNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
						}
						else
						{
							rootTypeNode = rootTypeNode.ReplaceNode(node, rewNode.Transformed);
						}
					}
				}
				else if (typeResult.Conversion == TypeConversion.Ignore)
				{
					rootTypeNode = rootTypeNode.RemoveNodeKeepDirectives(transformResult.Annotation, transformResult.LeadingWhitespaceTrivia);
				}
				else if (typeResult.Conversion == TypeConversion.Partial)
				{
					// First we need to remove ignored method
					var typeNode = rootTypeNode.GetAnnotatedNodes(transformResult.Annotation).OfType<TypeDeclarationSyntax>().First();
					// We need to remove the attributes as they cannot be defined in both partial classes
					var newTypeNode = typeNode.AddPartial().WithoutAttributes();
					var memberWhitespace = Whitespace(transformResult.LeadingWhitespaceTrivia.ToFullString() + transformResult.IndentTrivia.ToFullString());

					// We need to remove all other members that are not methods or types
					newTypeNode = newTypeNode.RemoveMembersKeepDirectives(o => !(o is MethodDeclarationSyntax || o is TypeDeclarationSyntax), memberWhitespace);

					foreach (var methodTransform in transformResult.TransformedMethods.OrderByDescending(o => o.OriginalStartSpan))
					{
						if (methodTransform.AnalyzationResult.Conversion == MethodConversion.Ignore)
						{
							// We need to add a whitespace trivia to keept directives as they will not have any leading whitespace
							newTypeNode = newTypeNode.RemoveNodeKeepDirectives(methodTransform.Annotation, memberWhitespace);
							continue;
						}
						var methodNode = newTypeNode.GetAnnotatedNodes(methodTransform.Annotation)
							.OfType<MethodDeclarationSyntax>()
							.First();
						var transformedNode = TransformMethod(methodTransform, transformResult, namespaceMetadata, methodNode);
						if (transformedNode.Transformed != null)
						{
							foreach (var transformer in _configuration.MethodTransformers)
							{
								var methodTransformResult = transformer.Transform(transformedNode, transformResult, namespaceMetadata);
								if (methodTransformResult == MethodTransformerResult.Skip)
								{
									continue;
								}
								transformedNode.Transformed = methodTransformResult.TransformedNode ?? transformedNode.Transformed;
								if (methodTransformResult.Fields != null)
								{
									if (transformedNode.Fields == null)
									{
										transformedNode.Fields = new List<FieldDeclarationSyntax>(1);
									}
									transformedNode.Fields.AddRange(methodTransformResult.Fields);
									// Update member names  for next transformators
									foreach (var variable in methodTransformResult.Fields.SelectMany(o => o.Declaration.Variables))
									{
										transformResult.MemberNames = transformResult.MemberNames.Add(variable.Identifier.Text);
									}
								}
								if (methodTransformResult.Methods != null)
								{
									transformedNode.AddMethods(methodTransformResult.Methods);
									// Update member names for next transformators
									foreach (var method in methodTransformResult.Methods)
									{
										transformResult.MemberNames = transformResult.MemberNames.Add(method.Identifier.Text);
									}
								}
							}
						}
						newTypeNode = newTypeNode.ReplaceWithMembers(methodNode, methodTransform.Transformed, methodTransform.Fields, methodTransform.Methods);
					}

					//foreach (var methodTransform in transformResult.TransformedMethods.Where(o => o.AnalyzationResult.Conversion == MethodConversion.Ignore)
					//	.OrderByDescending(o => o.OriginalStartSpan))
					//{
					//	var methodNode = newTypeNode.GetAnnotatedNodes(methodTransform.Annotation).First();
					//	newTypeNode = newTypeNode.RemoveNode(methodNode, SyntaxRemoveOptions.KeepUnbalancedDirectives);
					//}
					//var newNodes = transformResult.TransformedNodes
					//		.Union(transformResult.TransformedMethods.Where(o => o.AnalyzationResult.Conversion != MethodConversion.Ignore))
					//		.OrderBy(o => o.OriginalStartSpan)
					//		.SelectMany(o => o.GetTransformedNodes())
					//	.Union(typeNode.DescendantNodes().OfType<TypeDeclarationSyntax>())
					//	.ToList();
					// Add fields for async lock if any. We need a lock field for each synchronized method
					//foreach (var methodTransform in transformResult.TransformedMethods.Where(o => o.AsyncLockField != null).OrderBy(o => o.OriginalNode.SpanStart))
					//{
					//	newNodes.Insert(0, methodTransform.AsyncLockField);
					//}
					//newTypeNode = newTypeNode.WithMembers(List(newNodes));

					// Add the <content> instead of <summary> tag
					newTypeNode = newTypeNode.WithXmlContentTrivia(transformResult.EndOfLineTrivia, transformResult.LeadingWhitespaceTrivia);
					transformResult.Transformed = newTypeNode;
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, newTypeNode);
				}
			}

			rootTransformResult.Transformed = rootTypeNode;

			return rootTransformResult;
		}


	}
}
