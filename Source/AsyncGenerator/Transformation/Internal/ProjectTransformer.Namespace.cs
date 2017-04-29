using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		private RootNamespaceTransformationResult TransformNamespace(INamespaceAnalyzationResult rootResult)
		{
			var rootNode = rootResult.Node;
			var endOfLineTrivia = rootNode.DescendantTrivia().First(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
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
				NamespaceTransformationResult transformResult;
				if (node == rootNode)
				{
					transformResult = rootTransformResult;
				}
				else
				{
					transformResult = new NamespaceTransformationResult(result);
					rootNode = rootNode.ReplaceNode(node, node.WithAdditionalAnnotations(new SyntaxAnnotation(transformResult.Annotation)));
					rootTransformResult.DescendantTransformedNamespaces.Add(transformResult);
				}
				//transformResult.LeadingWhitespaceTrivia = node.GetLeadingTrivia().First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));

				foreach (var typeResult in result.Types.Where(o => o.Conversion != TypeConversion.Ignore))
				{
					var typeSpanStart = typeResult.Node.SpanStart - startRootSpan;
					var typeSpanLength = typeResult.Node.Span.Length;
					var typeNode = rootNode.DescendantNodesAndSelf()
						.OfType<TypeDeclarationSyntax>()
						.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
					var transformTypeResult = TransformType(typeResult);
					transformResult.TransformedTypes.Add(transformTypeResult);
					rootNode = rootNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformTypeResult.Annotation)));

					transformResult.ThreadingUsingRequired |= typeResult.Methods.Any(o => o.CancellationTokenRequired);
					transformResult.SystemUsingRequired |= typeResult.Methods.Any(o => o.WrapInTryCatch);
				}
			}

			// Save the orignal node that was only annotated
			var originalAnnotatedNode = rootNode;

			// Now we can start transforming the namespace. Start from the bottom in order to preserve replaced nested namespaces
			foreach (var transformResult in rootTransformResult.GetSelfAndDescendantTransformedNamespaces().OrderByDescending(o => o.OriginalNode.SpanStart))
			{
				var result = transformResult.AnalyzationResult;
				var node = rootNode.GetAnnotatedNodes(transformResult.Annotation).OfType<NamespaceDeclarationSyntax>().First();
				var newMembers = transformResult.TransformedTypes
					.OrderBy(o => o.OriginalStartSpan)
					.SelectMany(o => o.GetTransformedNodes())
					.Union(node.DescendantNodes().Where(o => o is NamespaceDeclarationSyntax)) // We need to include the already transformed namespaces
					.ToList();
				if (!newMembers.Any())
				{
					//TODO: fix regions
					rootNode = rootNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
					continue;
				}
				var newNode = node.WithMembers(List(newMembers));
				var leadingTrivia = result.Types.First().Node.GetLeadingTrivia().First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));
				//TODO: check if Task is conflicted inside namespace
				if (!rootResult.Node.HasUsing("System.Threading.Tasks"))
				{
					newNode = newNode.AddUsing("System.Threading.Tasks", TriviaList(leadingTrivia), endOfLineTrivia);
				}
				if (transformResult.ThreadingUsingRequired && !rootResult.Node.HasUsing("System.Threading"))
				{
					newNode = newNode.AddUsing("System.Threading", TriviaList(leadingTrivia), endOfLineTrivia);
				}
				if (transformResult.SystemUsingRequired && !rootResult.Node.HasUsing("System"))
				{
					newNode = newNode.AddUsing("System", TriviaList(leadingTrivia), endOfLineTrivia);
				}
				// TODO: add locking namespaces

				//TODO: fix regions
				rootNode = rootNode.ReplaceNode(node, newNode);

				// We need to update the original types if they were modified
				foreach (var transformTypeResult in transformResult.TransformedTypes.Where(o => o.OriginalModifiedNode != null).OrderByDescending(o => o.OriginalStartSpan))
				{
					if (rootTransformResult.OriginalModifiedNode == null)
					{
						rootTransformResult.OriginalModifiedNode = originalAnnotatedNode;
					}
					rootTransformResult.OriginalModifiedNode = rootTransformResult.OriginalModifiedNode
						.ReplaceNode(rootTransformResult.OriginalModifiedNode
							.GetAnnotatedNodes(transformTypeResult.Annotation).First(), transformTypeResult.OriginalModifiedNode);
				}
			}
			rootTransformResult.TransformedNode = rootNode;
			return rootTransformResult;
		}

	}
}
