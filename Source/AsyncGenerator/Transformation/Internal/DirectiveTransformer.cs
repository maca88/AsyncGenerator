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

	// A directive can be located on a member declaration within its leading trivia or its close brace token or on a end of line token
	internal class DirectiveTransformer
	{
		private CompilationUnitSyntax _rootNode;
		private IDocumentAnalyzationResult _documentResult;
		private static readonly HashSet<SyntaxKind> DirectiveKinds = new HashSet<SyntaxKind>
		{
			SyntaxKind.RegionDirectiveTrivia,
			SyntaxKind.EndRegionDirectiveTrivia,

			SyntaxKind.IfDirectiveTrivia,
			SyntaxKind.ElifDirectiveTrivia,
			SyntaxKind.ElseDirectiveTrivia,
			SyntaxKind.EndIfDirectiveTrivia,
		};

		public DirectiveTransformer(CompilationUnitSyntax rootNode, IDocumentAnalyzationResult documentResult)
		{
			_rootNode = rootNode;
			_documentResult = documentResult;
		}
		public CompilationUnitSyntax Analyze()
		{
			RegionTreeNode rootRegionNode = null;
			foreach (var directive in _rootNode.DescendantTrivia().Where(o => DirectiveKinds.Contains(o.Kind())).OrderBy(o => o.SpanStart))
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
				return _rootNode;
			}


			var namespaces = _documentResult.Namespaces
				.SelectMany(o => o.GetSelfAndDescendantsNamespaces())
				.OrderByDescending(o => o.Node.SpanStart)
				.ToDictionary(o => o, o => o.Types
					.SelectMany(t => t.GetSelfAndDescendantsTypes())
					.OrderByDescending(t => t.Node.SpanStart)
					.ToList());

			IMemberAnalyzationResult GetRelatedData(SyntaxTrivia directive)
			{
				var ns = namespaces.Keys.FirstOrDefault(o => o.Node.FullSpan.Contains(directive.Span));
				if (ns == null)
				{
					return null;
				}
				if (ns.Node.GetLeadingTrivia().FullSpan.Contains(directive.Span) || ns.Node.CloseBraceToken.LeadingTrivia.Any(o => o.FullSpan.Contains(directive.Span)))
				{
					return ns;
				}
				var type = namespaces[ns].FirstOrDefault(o => o.Node.FullSpan.Contains(directive.Span));
				if (type == null)
				{
					return null;
				}
				if (type.Node.GetLeadingTrivia().Span.Contains(directive.Span) || type.Node.CloseBraceToken.LeadingTrivia.Any(o => o.FullSpan.Contains(directive.Span)))
				{
					return type;
				}
				var method = type.Methods.FirstOrDefault(o => o.Node.GetLeadingTrivia().Span.Contains(directive.Span));
				return method;
			}

			// Find out where the regions are located, we need to find it by spans as the node parent will be always null
			foreach (var regionNode in rootRegionNode.GetSelfAndDescendantsNodes()
				.Where(o => o.Start.HasValue && o.End.HasValue))
			{
				var startRegionMember = GetRelatedData(regionNode.Start.Value);
				var endRegionMember = GetRelatedData(regionNode.End.Value);


			}



			return _rootNode;
		}

		

	}
}
#if DEBUG

#elif TRACE

#else

#endif