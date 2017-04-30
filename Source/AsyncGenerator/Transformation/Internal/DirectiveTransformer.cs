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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	internal class RegionTreeNode
	{
		public RegionTreeNode(SyntaxTrivia regionTrivia)
		{
			if (regionTrivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
			{
				Start = regionTrivia;
			}
			else if (regionTrivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
			{
				// This should happen only when regions are invalid
				End = regionTrivia;
			}
			else
			{
				throw new InvalidOperationException($"Invalid region trivia {regionTrivia}");
			}
		}

		public SyntaxTrivia? Start { get; set; }

		public SyntaxTrivia? End { get; set; }

		public RegionTreeNode Child { get; set; }

		public RegionTreeNode Next { get; set; }

		private bool IsCompleted()
		{
			if (Next != null)
			{
				return Next.IsCompleted();
			}
			if (End.HasValue)
			{
				return true;
			}
			return false;
		}

		public void AddNode(SyntaxTrivia regionTrivia)
		{
			switch (regionTrivia.Kind())
			{
				case SyntaxKind.RegionDirectiveTrivia:
					// Check if the region is a child or a sibling by checking if the current node has an end region
					var node = End.HasValue ? Next : Child;
					if (node != null)
					{
						node.AddNode(regionTrivia);
					}
					else 
					{
						if (End.HasValue)
						{
							Next = new RegionTreeNode(regionTrivia);
						}
						else
						{
							Child = new RegionTreeNode(regionTrivia);
						}
					}
					break;
				case SyntaxKind.EndRegionDirectiveTrivia:
					if (End.HasValue)
					{
						if (Next == null)
						{
							// This should happen only when regions are invalid 
							Next = new RegionTreeNode(regionTrivia);
						}
						else
						{
							Next.AddNode(regionTrivia);
						}
					}
					// We can set the current end only when all children have an end set or there are no children
					else if (Child == null || Child.IsCompleted())
					{
						End = regionTrivia;
					}
					else if (Child == null)
					{
						// This should happen only when regions are invalid 
						Child = new RegionTreeNode(regionTrivia);
					}
					else
					{
						Child.AddNode(regionTrivia);
					}
					break;
				default:
					throw new InvalidOperationException($"Invalid region trivia {regionTrivia}");
			}
		}

		public IEnumerable<RegionTreeNode> GetSelfAndDescendantsNodes()
		{
			return GetSelfAndDescendantsNodesRecursively(this);
		}

		private static IEnumerable<RegionTreeNode> GetSelfAndDescendantsNodesRecursively(RegionTreeNode node)
		{
			yield return node;
			if (node.Child != null)
			{
				foreach (var childNode in node.Child.GetSelfAndDescendantsNodes())
				{
					yield return childNode;
				}
			}
			if (node.Next == null)
			{
				yield break;
			}
			foreach (var nextNode in node.Next.GetSelfAndDescendantsNodes())
			{
				yield return nextNode;
			}
		}
	}

	/// <summary>
	/// A transformator that takes care of partially generated directives.
	/// DEPRECATED: not needed anymore as there is an easier way to achieve the same result
	/// </summary>
	internal class DirectiveTransformer
	{
		private static readonly HashSet<SyntaxKind> DirectiveKinds = new HashSet<SyntaxKind>
		{
			SyntaxKind.RegionDirectiveTrivia,
			SyntaxKind.EndRegionDirectiveTrivia,

			SyntaxKind.IfDirectiveTrivia,
			SyntaxKind.ElifDirectiveTrivia,
			SyntaxKind.ElseDirectiveTrivia,
			SyntaxKind.EndIfDirectiveTrivia,
		};

		private class RelatedTransformationResult
		{
			public RelatedTransformationResult(ITransformationResult related)
			{
				Related = related;
			}

			public ITransformationResult Related { get; }

			public ITransformationResult NextRelated { get; set; }

			public ITransformationResult PreviousRelated { get; set; }
		}

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformResult)
		{
			var rootNode = transformResult.Transformed;
			var replaceMetadata = new TriviaReplaceMetadata();

			var origRootNode = transformResult.Original;
			// Step 1 - Find all directives and link them together
			RegionTreeNode rootRegionNode = null;
			foreach (var directive in origRootNode.DescendantTrivia().Where(o => DirectiveKinds.Contains(o.Kind())).OrderBy(o => o.SpanStart))
			{
				if (directive.IsKind(SyntaxKind.RegionDirectiveTrivia) || directive.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					if (rootRegionNode == null)
					{
						rootRegionNode = new RegionTreeNode(directive);
					}
					else
					{
						rootRegionNode.AddNode(directive);
					}
				}
				else
				{
					// TODO: if else elif ifend
				}
			}

			if (rootRegionNode == null)
			{
				return transformResult.Transformed;
			}

			var allTransformResults = transformResult.TransformedNamespaces
				.Cast<IMemberTransformationResult>()
				.Union(transformResult.TransformedTypes)
				.Union(transformResult.TransformedNamespaces.SelectMany(o => o.TransformedTypes))
				.Union(transformResult.TransformedNamespaces.SelectMany(o => o.TransformedTypes)
					.SelectMany(o => o.TransformedMethods))
				.ToDictionary(o => o.GetAnalyzationResult(), o => o);

			ITransformationResult GetNextTransformationResult(IMemberAnalyzationResult result)
			{
				var nextResult = result.GetNext();
				while (nextResult != null)
				{
					if (allTransformResults.ContainsKey(nextResult))
					{
						return allTransformResults[nextResult];
					}
					nextResult = nextResult.GetNext();
				}
				return transformResult;
			}

			// Get related analyzation result
			IAnalyzationResult GetRelatedAnalyzationResult(SyntaxTrivia directive)
			{
				// Check if the directive is located before an end of line
				if (transformResult.Original.EndOfFileToken.LeadingTrivia.FullSpan.Contains(directive.Span))
				{
					return transformResult.AnalyzationResult;
				}
				var result = transformResult.AnalyzationResult;
				var globalType = result.GlobalNamespace.Types.FirstOrDefault(o =>
					o.Node.GetLeadingTrivia().FullSpan.Contains(directive.Span) ||
					o.Node.CloseBraceToken.LeadingTrivia.Any(t => t.FullSpan.Contains(directive.Span)));
				if (globalType != null)
				{
					return globalType;
				}

				foreach (var namespaceResult in result.Namespaces.SelectMany(o => o.GetSelfAndDescendantsNamespaces()))
				{
					if (
						namespaceResult.Node.GetLeadingTrivia().FullSpan.Contains(directive.Span) ||
						namespaceResult.Node.CloseBraceToken.LeadingTrivia.Any(o => o.FullSpan.Contains(directive.Span)))
					{
						return namespaceResult;
					}

					foreach (var typeResult in namespaceResult.Types.SelectMany(o => o.GetSelfAndDescendantsTypes()))
					{
						if (
							typeResult.Node.GetLeadingTrivia().FullSpan.Contains(directive.Span) ||
							typeResult.Node.CloseBraceToken.LeadingTrivia.Any(o => o.FullSpan.Contains(directive.Span)))
						{
							return typeResult;
						}
						var method = typeResult.Methods.FirstOrDefault(o => o.Node.GetLeadingTrivia().FullSpan.Contains(directive.Span));
						if (method != null)
						{
							return method;
						}
					}
				}
				// The trivia is outside of our generation scope
				return null;
			}

			// Find out where the regions are located, we need to find it by spans as the node parent will be always null
			foreach (var regionNode in rootRegionNode.GetSelfAndDescendantsNodes()
				.Where(o => o.Start.HasValue && o.End.HasValue))
			{
				var startRegionAnalyzeResult = GetRelatedAnalyzationResult(regionNode.Start.Value);
				var endRegionAnalyzeResult = GetRelatedAnalyzationResult(regionNode.End.Value);
				if (startRegionAnalyzeResult == null && endRegionAnalyzeResult == null)
				{
					continue; // We do not need to do anything as both start and end region are outside of our generation scope
				}
				if (startRegionAnalyzeResult is IDocumentAnalyzationResult &&
				    endRegionAnalyzeResult is IDocumentAnalyzationResult)
				{
					continue; // Both directives will get generated, skip further processing
				}

				var startRegionMemberAnalyzeResult = startRegionAnalyzeResult as IMemberAnalyzationResult;
				var endRegionMemberAnalyzeResult = endRegionAnalyzeResult as IMemberAnalyzationResult;

				if (startRegionAnalyzeResult is IDocumentAnalyzationResult && 
					endRegionMemberAnalyzeResult != null && 
					allTransformResults.ContainsKey(endRegionMemberAnalyzeResult))
				{
					continue; // Both directives will get generated, skip further processing
				}

				if (endRegionAnalyzeResult is IDocumentAnalyzationResult &&
				    startRegionMemberAnalyzeResult != null &&
				    allTransformResults.ContainsKey(startRegionMemberAnalyzeResult))
				{
					continue; // Both directives will get generated, skip further processing
				}

				if (startRegionMemberAnalyzeResult != null && endRegionMemberAnalyzeResult != null)
				{
					if (allTransformResults.ContainsKey(startRegionMemberAnalyzeResult) &&
					    allTransformResults.ContainsKey(endRegionMemberAnalyzeResult))
					{
						continue; // Both directives will get generated, skip further processing
					}

					if (!allTransformResults.ContainsKey(startRegionMemberAnalyzeResult) &&
					    !allTransformResults.ContainsKey(endRegionMemberAnalyzeResult))
					{
						continue; // Both directives will not get generated, skip further processing
					}
				}

				void AddTrivia(IMemberAnalyzationResult regionMemberAnalyzeResult, SyntaxTrivia region, TriviaReplaceMetadata metadata)
				{
					if (regionMemberAnalyzeResult == null || allTransformResults.ContainsKey(regionMemberAnalyzeResult))
					{
						return;
					}
					// We need to find the next node where we will append the directive
					var nextTransformResult = GetNextTransformationResult(regionMemberAnalyzeResult);
					var nextMemberTransform = nextTransformResult as IMemberTransformationResult;
					if (nextMemberTransform == null)
					{
						// Prepend the trivia at the end of the document
						var leadingTrivia = TriviaList(
							region,
							rootNode.GetEndOfLine()
						);
						metadata.EndOfFileTokenTriviaList = metadata.EndOfFileTokenTriviaList.AddRange(leadingTrivia);

						/*
						rootNode = rootNode.ReplaceToken(rootNode.EndOfFileToken, rootNode.EndOfFileToken
							.WithLeadingTrivia(leadingTrivia.AddRange(rootNode.EndOfFileToken.LeadingTrivia)));*/
					}
					else
					{
						// We need to check if the next node is the parent as we need to know where to append the trivia on the start or end of the node
						var nextAnalyzeNode = nextMemberTransform.GetAnalyzationResult();
						//var nextNode = rootNode.GetAnnotatedNodes(nextMemberTransform.Annotation).First();
						var leadingTrivia = TriviaList(
							Whitespace(nextMemberTransform.LeadingWhitespaceTrivia.ToFullString() +
							           nextMemberTransform.IndentTrivia.ToFullString()),
							region,
							nextMemberTransform.EndOfLineTrivia
						);
						if (regionMemberAnalyzeResult.IsParent(nextAnalyzeNode))
						{
							// Prepend the directive on the close brace of the parent node
							if (metadata.NodeCloseBraceTrivias.TryGetValue(nextMemberTransform.Annotation, out var triviaList))
							{
								metadata.NodeCloseBraceTrivias[nextMemberTransform.Annotation] = triviaList.AddRange(leadingTrivia);
							}
							else
							{
								metadata.NodeCloseBraceTrivias.Add(nextMemberTransform.Annotation, leadingTrivia);
							}
							//rootNode = rootNode.ReplaceNode(nextNode, nextNode.PrependCloseBraceLeadingTrivia(leadingTrivia));
						}
						else
						{
							// Prepend the directive on the lead trivia of the next node
							if (metadata.NodeTrivias.TryGetValue(nextMemberTransform.Annotation, out var triviaList))
							{
								metadata.NodeTrivias[nextMemberTransform.Annotation] = triviaList.AddRange(leadingTrivia);
							}
							else
							{
								metadata.NodeTrivias.Add(nextMemberTransform.Annotation, leadingTrivia);
							}
							//rootNode = rootNode.ReplaceNode(nextNode, nextNode.WithLeadingTrivia(leadingTrivia.AddRange(nextNode.GetLeadingTrivia())));
						}
					}
				}

				AddTrivia(endRegionMemberAnalyzeResult, regionNode.End.Value, replaceMetadata);
				AddTrivia(startRegionMemberAnalyzeResult, regionNode.Start.Value, replaceMetadata);
			}

			foreach (var nodeTrivias in replaceMetadata.NodeCloseBraceTrivias)
			{
				var nextNode = rootNode.GetAnnotatedNodes(nodeTrivias.Key).First();
				rootNode = rootNode.ReplaceNode(nextNode, nextNode.PrependCloseBraceLeadingTrivia(nodeTrivias.Value));
			}
			foreach (var nodeTrivias in replaceMetadata.NodeTrivias)
			{
				var nextNode = rootNode.GetAnnotatedNodes(nodeTrivias.Key).First();
				rootNode = rootNode.ReplaceNode(nextNode, nextNode.WithLeadingTrivia(nodeTrivias.Value.AddRange(nextNode.GetLeadingTrivia())));
			}

			rootNode = rootNode.ReplaceToken(rootNode.EndOfFileToken, rootNode.EndOfFileToken
				.WithLeadingTrivia(replaceMetadata.EndOfFileTokenTriviaList.AddRange(rootNode.EndOfFileToken.LeadingTrivia)));

			return null;
		}


		private class TriviaReplaceMetadata
		{
			public Dictionary<string, SyntaxTriviaList> NodeTrivias { get; } = new Dictionary<string, SyntaxTriviaList>();

			public Dictionary<string, SyntaxTriviaList> NodeCloseBraceTrivias { get; } = new Dictionary<string, SyntaxTriviaList>();

			public SyntaxTriviaList EndOfFileTokenTriviaList { get; set; }
		}
	}
}