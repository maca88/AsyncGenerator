using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private RootTypeTransformationResult TransformType(ITypeAnalyzationResult rootTypeResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			var anyMissingMembers = rootTypeResult.Conversion != TypeConversion.Partial && rootTypeResult.GetSelfAndDescendantsTypes()
				.Any(o => o.MethodsAndAccessors.Any(m => m.Missing));
			var result = TransformType(rootTypeResult, namespaceMetadata, false);
			if (anyMissingMembers)
			{
				result.Partial = TransformType(rootTypeResult, namespaceMetadata, true);
				var transformedNode = result.Partial.Transformed;
				// We need to remove all directives
				while (transformedNode.ContainsDirectives)
				{
					transformedNode = transformedNode.RemoveNode(transformedNode.GetFirstDirective(), SyntaxRemoveOptions.KeepNoTrivia);
				}
				result.Partial.Transformed = transformedNode;
				result.OriginalModified = result.Partial.OriginalModified;
			}
			return result;
		}

		private RootTypeTransformationResult TransformType(ITypeAnalyzationResult rootTypeResult, INamespaceTransformationMetadata namespaceMetadata, bool onlyMissingMembers)
		{
			var rootTypeNode = rootTypeResult.Node;
			var startRootTypeSpan = rootTypeNode.SpanStart;
			var rootTransformResult = new RootTypeTransformationResult(rootTypeResult)
			{
				MemberNames = rootTypeResult.Symbol.MemberNames.ToImmutableHashSet()
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
				if (typeResult.Conversion == TypeConversion.NewType || typeResult.Conversion == TypeConversion.Copy)
				{
					foreach (var typeReference in typeResult.TypeReferences)
					{
						var reference = typeReference.ReferenceLocation;
						var refSpanStart = reference.Location.SourceSpan.Start - startRootTypeSpan;
						var refSpanLength = reference.Location.SourceSpan.Length;
						var nameNode = rootTypeNode.GetSimpleName(refSpanStart, refSpanLength, typeReference.IsCref);
						var transformedNode = new TransformationResult(nameNode)
						{
							Transformed = nameNode.WithIdentifier(Identifier(nameNode.Identifier.ValueText + "Async").WithTriviaFrom(nameNode.Identifier))
						};
						transformResult.TransformedNodes.Add(transformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
					}
					
					foreach (var field in typeResult.Fields)
					{
						var fieldSpanStart = field.Node.SpanStart - startRootTypeSpan;
						var fieldSpanLength = field.Node.Span.Length;
						var fieldNode = rootTypeNode.DescendantNodes()
							.OfType<BaseFieldDeclarationSyntax>()
							.First(o => o.SpanStart == fieldSpanStart && o.Span.Length == fieldSpanLength);
						// TODO: move to sepatate file
						var transformedNode = new TransformationResult(fieldNode);
						var newFieldNode = fieldNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation));
						var startFieldSpan = fieldNode.SpanStart + startRootTypeSpan;
						startFieldSpan -= newFieldNode.SpanStart;

						SyntaxNode transformed;
						if (field.Variables.All(v => v.Conversion == FieldVariableConversion.Ignore)) // Remove all unused fields
						{
							transformed = null;
						}
						else
						{
							foreach (var typeReference in field.TypeReferences)
							{
								var reference = typeReference.ReferenceLocation;
								var refSpanStart = reference.Location.SourceSpan.Start - startFieldSpan;
								var refSpanLength = reference.Location.SourceSpan.Length;
								var nameNode = newFieldNode.GetSimpleName(refSpanStart, refSpanLength, typeReference.IsCref);

								newFieldNode = newFieldNode.ReplaceNode(nameNode,
									nameNode.WithIdentifier(
										Identifier(nameNode.Identifier.ValueText + "Async").WithTriviaFrom(nameNode.Identifier)));
							}
							transformed = newFieldNode;
						}
						transformedNode.Transformed = transformed;

						transformResult.TransformedNodes.Add(transformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(fieldNode, fieldNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
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

					// Annotate all locks inside the method before the transformations begins as will be easier to transform them if needed
					foreach (var lockData in methodResult.Locks)
					{
						var lockSpanStart = lockData.Node.SpanStart - startRootTypeSpan;
						var lockSpanLength = lockData.Node.Span.Length;
						var lockNode = rootTypeNode.DescendantNodes()
							.OfType<LockStatementSyntax>()
							.First(o => o.SpanStart == lockSpanStart && o.Span.Length == lockSpanLength);
						var lockTransformedNode = new LockTransformationResult(lockData);
						transformedNode.TransformedLocks.Add(lockTransformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(lockNode,
							lockNode.WithAdditionalAnnotations(new SyntaxAnnotation(lockTransformedNode.Annotation)));
					}
				}

				foreach (var methodResult in typeResult.SpecialMethods)
				{
					var methodSpanStart = methodResult.GetNode().SpanStart - startRootTypeSpan;
					var methodSpanLength = methodResult.GetNode().Span.Length;
					var methodNode = rootTypeNode.DescendantNodes()
						.OfType<BaseMethodDeclarationSyntax>()
						.First(o => o.SpanStart == methodSpanStart && o.Span.Length == methodSpanLength);
					var transformedNode = new FunctionTransformationResult(methodResult);
					transformResult.TransformedSpecialMethods.Add(transformedNode);
					rootTypeNode = rootTypeNode.ReplaceNode(methodNode, methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
				}


				foreach (var propertyResult in typeResult.Properties)
				{
					var spanStart = propertyResult.Node.SpanStart - startRootTypeSpan;
					var spanLength = propertyResult.Node.Span.Length;
					var node = rootTypeNode.DescendantNodes()
						.OfType<PropertyDeclarationSyntax>()
						.First(o => o.SpanStart == spanStart && o.Span.Length == spanLength);
					var transformedNode = new PropertyTransformationResult(propertyResult);
					transformResult.TransformedProperties.Add(transformedNode);
					rootTypeNode = rootTypeNode.ReplaceNode(node, node.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
				}
			}
			// Save the orignal node that was only annotated
			var originalAnnotatedNode = rootTypeNode;

			// Now we can start transforming the type. Start from the bottom in order to preserve replaced nested types
			foreach (var transformResult in rootTransformResult.GetSelfAndDescendantTransformedTypes().OrderByDescending(o => o.OriginalNode.SpanStart))
			{
				var typeResult = transformResult.AnalyzationResult;
				
				// Add partial keyword on the original node if not present
				if ((typeResult.Conversion == TypeConversion.Partial || onlyMissingMembers) && !typeResult.IsPartial)
				{
					if (rootTransformResult.OriginalModified == null)
					{
						rootTransformResult.OriginalModified = originalAnnotatedNode;
					}
					var typeNode = rootTransformResult.OriginalModified.GetAnnotatedNodes(transformResult.Annotation).OfType<TypeDeclarationSyntax>().First();
					rootTransformResult.OriginalModified = rootTransformResult.OriginalModified.ReplaceNode(typeNode, typeNode.AddPartial());
				}
				if (typeResult.Conversion == TypeConversion.Ignore)
				{
					rootTypeNode = rootTypeNode.RemoveNodeKeepDirectives(transformResult.Annotation, transformResult.LeadingWhitespaceTrivia);
					continue;
				}

				var memberWhitespace = Whitespace(transformResult.LeadingWhitespaceTrivia.ToFullString() + transformResult.IndentTrivia.ToFullString());

				if (typeResult.Conversion == TypeConversion.Partial || onlyMissingMembers)
				{
					// First we need to remove ignored method
					var typeNode = rootTypeNode.GetAnnotatedNodes(transformResult.Annotation).OfType<TypeDeclarationSyntax>().First();
					// We need to remove the attributes as they cannot be defined in both partial classes
					var newTypeNode = typeNode.AddPartial().WithoutAttributes();

					// We need to remove all other members that are not methods, properties or types
					newTypeNode = newTypeNode.RemoveMembersKeepDirectives(o => 
						!(o is BaseMethodDeclarationSyntax || o is TypeDeclarationSyntax || o is PropertyDeclarationSyntax), memberWhitespace);
					newTypeNode = TransformMethodsAndProperties(newTypeNode, transformResult, namespaceMetadata, memberWhitespace, onlyMissingMembers);

					// Add the <content> instead of <summary> tag
					newTypeNode = newTypeNode.WithXmlContentTrivia(transformResult.EndOfLineTrivia, transformResult.LeadingWhitespaceTrivia);
					transformResult.Transformed = newTypeNode;
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, newTypeNode);
				}
				// If the root type has to be a new type then all nested types have to be new types
				else if (typeResult.Conversion == TypeConversion.NewType || typeResult.Conversion == TypeConversion.Copy)
				{
					var typeNode = rootTypeNode.GetAnnotatedNodes(transformResult.Annotation).OfType<TypeDeclarationSyntax>().First();
					var identifierToken = typeNode.ChildTokens().First(o => o.IsKind(SyntaxKind.IdentifierToken));
					var newTypeNode = typeResult.Conversion == TypeConversion.NewType
						? typeNode.ReplaceToken(identifierToken, Identifier(identifierToken.ValueText + "Async").WithTriviaFrom(identifierToken))
						: typeNode;
					
					// Replace all rewritten nodes
					foreach (var rewNode in transformResult.TransformedNodes)
					{
						var node = newTypeNode.GetAnnotatedNodes(rewNode.Annotation).First();
						if (rewNode.Transformed == null)
						{
							newTypeNode = newTypeNode.RemoveNode(node, SyntaxRemoveOptions.KeepUnbalancedDirectives);
						}
						else
						{
							newTypeNode = newTypeNode.ReplaceNode(node, rewNode.Transformed);
						}
					}
					// Replace ctor names
					foreach (var newToken in transformResult.TransformedTokens)
					{
						newTypeNode = newTypeNode.ReplaceToken(newTypeNode.GetAnnotatedTokens(newToken.Key).First(), newToken.Value);
					}

					newTypeNode = TransformMethodsAndProperties(newTypeNode, transformResult, namespaceMetadata, memberWhitespace, false);
					transformResult.Transformed = newTypeNode;
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, newTypeNode);
				}
			}

			rootTransformResult.Transformed = rootTypeNode;
			return rootTransformResult;
		}

		private TypeDeclarationSyntax TransformMethodsAndProperties(TypeDeclarationSyntax newTypeNode,
			TypeTransformationResult transformResult,
			INamespaceTransformationMetadata namespaceMetadata, SyntaxTrivia memberWhitespace, bool onlyMissingMembers)
		{
			foreach (var transform in transformResult.TransformedMethods
				.Cast<AnnotatedNode>()
				.Union(transformResult.TransformedProperties)
				.Union(transformResult.TransformedSpecialMethods)
				.OrderByDescending(o => o.OriginalStartSpan))
			{
				if (transform is MethodTransformationResult methodTransform)
				{
					newTypeNode = TransformMethod(methodTransform, newTypeNode, transformResult, namespaceMetadata, memberWhitespace, onlyMissingMembers);
				}
				else if (transform is PropertyTransformationResult propertyTransform)
				{
					newTypeNode = TransformProperty(propertyTransform, newTypeNode, transformResult, namespaceMetadata, memberWhitespace, onlyMissingMembers);
				}
				else if (transform is FunctionTransformationResult functionTransform)
				{
					newTypeNode = TransformSpecialMethod(functionTransform, newTypeNode, transformResult, namespaceMetadata, memberWhitespace, onlyMissingMembers);
				}
			}
			return newTypeNode;
		}

		private TypeDeclarationSyntax TransformSpecialMethod(FunctionTransformationResult methodTransform,
			TypeDeclarationSyntax newTypeNode, TypeTransformationResult transformResult,
			INamespaceTransformationMetadata namespaceMetadata, SyntaxTrivia memberWhitespace, bool onlyMissingMembers)
		{
			if (methodTransform.AnalyzationResult.Conversion == MethodConversion.Ignore)
			{
				// We need to add a whitespace trivia to keep directives as they will not have any leading whitespace
				newTypeNode = newTypeNode.RemoveNodeKeepDirectives(methodTransform.Annotation, memberWhitespace);
				return newTypeNode;
			}
			var methodNode = newTypeNode.GetAnnotatedNodes(methodTransform.Annotation)
				.OfType<BaseMethodDeclarationSyntax>()
				.First();
			var transformedResult = TransformFunction(methodTransform.AnalyzationResult, transformResult, namespaceMetadata);
			var transformedNode = (BaseMethodDeclarationSyntax)transformedResult.Transformed;
			if (transformedNode is ConstructorDeclarationSyntax ctorNode && transformResult.AnalyzationResult.Conversion == TypeConversion.NewType)
			{
				transformedNode = ctorNode.WithIdentifier(Identifier(ctorNode.Identifier.ValueText + "Async")
					.WithTriviaFrom(ctorNode.Identifier));
			}
			newTypeNode = newTypeNode.ReplaceNode(methodNode, transformedNode);
			return newTypeNode;
		}

		private TypeDeclarationSyntax TransformMethod(MethodTransformationResult methodTransform, TypeDeclarationSyntax newTypeNode, TypeTransformationResult transformResult,
			INamespaceTransformationMetadata namespaceMetadata, SyntaxTrivia memberWhitespace, bool onlyMissingMembers)
		{
			if (methodTransform.AnalyzationResult.Conversion == MethodConversion.Ignore || (onlyMissingMembers && !methodTransform.AnalyzationResult.Missing))
			{
				// We need to add a whitespace trivia to keep directives as they will not have any leading whitespace
				newTypeNode = newTypeNode.RemoveNodeKeepDirectives(methodTransform.Annotation, memberWhitespace);
				return newTypeNode;
			}
			var methodNode = newTypeNode.GetAnnotatedNodes(methodTransform.Annotation)
				.OfType<MethodDeclarationSyntax>()
				.First();
			var transformedNode = TransformMethod(methodNode, !onlyMissingMembers, methodTransform, transformResult, namespaceMetadata);
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
			return newTypeNode;
		}

		private TypeDeclarationSyntax TransformProperty(PropertyTransformationResult propertyTransform, TypeDeclarationSyntax newTypeNode, TypeTransformationResult transformResult,
			INamespaceTransformationMetadata namespaceMetadata, SyntaxTrivia memberWhitespace, bool onlyMissingMembers)
		{
			var propertyNode = newTypeNode.GetAnnotatedNodes(propertyTransform.Annotation)
				.OfType<PropertyDeclarationSyntax>()
				.First();
			var transformedNode = TransformProperty(propertyNode, !onlyMissingMembers, propertyTransform, transformResult, namespaceMetadata);
			foreach (var transformedAccessor in transformedNode.TransformedAccessors.Where(o => o.Transformed != null))
			{
				foreach (var transformer in _configuration.MethodTransformers)
				{
					var methodTransformResult = transformer.Transform(transformedAccessor, transformResult, namespaceMetadata);
					if (methodTransformResult == MethodTransformerResult.Skip)
					{
						continue;
					}
					transformedAccessor.Transformed = methodTransformResult.TransformedNode ?? transformedAccessor.Transformed;
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
				propertyTransform.AddMethod(transformedAccessor.Transformed);
			}
			newTypeNode = newTypeNode.AppendMembers(propertyNode, propertyTransform.Fields, propertyTransform.Methods);
			// We need to remove the property when generating only the missing members
			if (onlyMissingMembers || propertyTransform.AnalyzationResult.Conversion == PropertyConversion.Ignore)
			{
				// We need to add a whitespace trivia to keep directives as they will not have any leading whitespace
				newTypeNode = newTypeNode.RemoveNodeKeepDirectives(propertyTransform.Annotation, memberWhitespace);
			}
			return newTypeNode;
		}



		
	}
}
