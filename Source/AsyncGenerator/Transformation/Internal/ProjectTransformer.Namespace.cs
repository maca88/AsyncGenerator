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
		private RootNamespaceTransformationResult TransformNamespace(INamespaceAnalyzationResult rootResult)
		{
			var rootNode = rootResult.Node;
			var startRootSpan = rootNode.SpanStart;
			var rootTransformResult = new RootNamespaceTransformationResult(rootResult);

			// We do this here because we want that the root node has span start equal to 0
			rootNode = rootNode.WithAdditionalAnnotations(new SyntaxAnnotation(rootTransformResult.Annotation));
			startRootSpan -= rootNode.SpanStart;

			// Before any modification we need to annotate nodes that will be transformed in order to find them later on.
			foreach (var result in rootResult.GetSelfAndDescendantsNamespaces())
			{
				var spanStart = result.Node.SpanStart - startRootSpan;
				var spanLength = result.Node.Span.Length;
				var node = rootNode.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>()
					.First(o => o.SpanStart == spanStart && o.Span.Length == spanLength);
				var leadingWhitespace = node.GetLeadingWhitespace();
				NamespaceTransformationResult transformResult;
				if (node == rootNode)
				{
					transformResult = rootTransformResult;
					// To get the indent of the root namespace we need to check the first memeber as the parent is the document
					transformResult.IndentTrivia = node.Members.FirstOrDefault()?.GetLeadingWhitespace() ?? default(SyntaxTrivia);
				}
				else
				{
					transformResult = new NamespaceTransformationResult(result)
					{
						IndentTrivia = node.GetIndent(leadingWhitespace)
					};
					rootNode = rootNode.ReplaceNode(node, node.WithAdditionalAnnotations(new SyntaxAnnotation(transformResult.Annotation)));
					rootTransformResult.DescendantTransformedNamespaces.Add(transformResult);
				}
				transformResult.LeadingWhitespaceTrivia = leadingWhitespace;
				transformResult.EndOfLineTrivia = node.GetEndOfLine();
				transformResult.TaskConflict = rootResult.ContainsType(nameof(Task)) || rootResult.ContainsType(nameof(ValueTask));
				transformResult.UsingSystem = result.Node.HasUsing("System");

				foreach (var typeResult in result.Types/*.Where(o => o.Conversion != TypeConversion.Ignore)*/)
				{
					var typeSpanStart = typeResult.Node.SpanStart - startRootSpan;
					var typeSpanLength = typeResult.Node.Span.Length;
					var typeNode = rootNode.DescendantNodesAndSelf()
						.OfType<TypeDeclarationSyntax>()
						.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
					var transformTypeResult = TransformType(typeResult, transformResult);
					transformResult.TransformedTypes.Add(transformTypeResult);
					rootNode = rootNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformTypeResult.Annotation)));

					transformResult.ThreadingUsingRequired |=
						typeResult.GetSelfAndDescendantsTypes().SelectMany(o => o.MethodsAndAccessors).Any(o => o.CancellationTokenRequired) ||
						typeResult.GetSelfAndDescendantsTypes()
							.SelectMany(o => o.MethodsAndAccessors)
							.SelectMany(o => o.GetSelfAndDescendantsFunctions())
							.Any(o => o.BodyFunctionReferences.Any(r => r.PassCancellationToken));
				}
			}

			// Save the orignal node that was only annotated
			var originalAnnotatedNode = rootNode;

			// Now we can start transforming the namespace. Start from the bottom in order to preserve replaced nested namespaces
			foreach (var transformResult in rootTransformResult.GetSelfAndDescendantTransformedNamespaces().OrderByDescending(o => o.OriginalNode.SpanStart))
			{
				if (transformResult.AnalyzationResult.Conversion == NamespaceConversion.Ignore)
				{
					rootNode = rootNode.RemoveNodeKeepDirectives(transformResult.Annotation, transformResult.LeadingWhitespaceTrivia,
						transformResult.EndOfLineTrivia);
					continue;
				}

				var node = rootNode.GetAnnotatedNodes(transformResult.Annotation).OfType<NamespaceDeclarationSyntax>().First();
				var newNode = node;
				var memberWhitespace = Whitespace(transformResult.LeadingWhitespaceTrivia.ToFullString() + transformResult.IndentTrivia.ToFullString());

				foreach (var transformType in transformResult.TransformedTypes.OrderByDescending(o => o.OriginalStartSpan))
				{
					if (transformType.AnalyzationResult.Conversion == TypeConversion.Ignore)
					{
						// We need to add a whitespace trivia to kept directives as they will not have any leading whitespace
						newNode = newNode.RemoveNodeKeepDirectives(transformType.Annotation, memberWhitespace, transformResult.EndOfLineTrivia);
						continue;
					}

					var typeNode = newNode.GetAnnotatedNodes(transformType.Annotation)
						.OfType<MemberDeclarationSyntax>()
						.First();
					newNode = newNode.ReplaceWithMembers(typeNode, transformType.GetTransformedNodes()
						.OfType<MemberDeclarationSyntax>()
						.ToImmutableList());
				}

				// We need to remove all other members that are not namespaces or types
				newNode = newNode.RemoveMembersKeepDirectives(o => !(o is NamespaceDeclarationSyntax || o is TypeDeclarationSyntax), memberWhitespace,
					transformResult.EndOfLineTrivia);

				// TODO: adding namespaces can introduce name conflicts, we should avoid it
				if (!transformResult.TaskConflict && !rootResult.Node.HasUsing("System.Threading.Tasks"))
				{
					transformResult.AddUsing("System.Threading.Tasks");
				}
				if (transformResult.ThreadingUsingRequired && !rootResult.Node.HasUsing("System.Threading"))
				{
					transformResult.AddUsing("System.Threading");
				}
				// TODO: add locking namespaces

				foreach (var additionalUsing in transformResult.AdditionalUsings)
				{
					if (!rootResult.Node.HasUsing(additionalUsing) && !node.HasUsing(additionalUsing))
					{
						newNode = newNode.AddUsing(additionalUsing, TriviaList(memberWhitespace), transformResult.EndOfLineTrivia);
					}
				}

				transformResult.Transformed = newNode;
				rootNode = rootNode.ReplaceNode(node, newNode);

				// We need to update the original types if they were modified
				foreach (var transformTypeResult in transformResult.TransformedTypes.Where(o => o.OriginalModified != null).OrderByDescending(o => o.OriginalStartSpan))
				{
					if (rootTransformResult.OriginalModified == null)
					{
						rootTransformResult.OriginalModified = originalAnnotatedNode;
					}
					rootTransformResult.OriginalModified = rootTransformResult.OriginalModified
						.ReplaceNode(rootTransformResult.OriginalModified
							.GetAnnotatedNodes(transformTypeResult.Annotation).First(), transformTypeResult.OriginalModified);
				}
			}
			rootTransformResult.Transformed = rootNode;
			return rootTransformResult;
		}

	}
}
