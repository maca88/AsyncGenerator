using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	public class EmptyRegionRemover : IDocumentTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}

		private class RegionInfo
		{
			public RegionInfo Parent { get; set; }

			public int? EndOfLineIndex { get; set; }

			public int? StartRegionIndex { get; set; }

			public int? StartRegionEndOfLineIndex { get; set; }

			public int? StartIndex => StartRegionEndOfLineIndex ?? StartRegionIndex;

			public int? EndIndex { get; set; }

			private bool IsValid => EndIndex.HasValue && StartIndex.HasValue;

			public RegionInfo GetValidRoot()
			{
				if (Parent?.IsValid == true)
				{
					return Parent.GetValidRoot();
				}
				return IsValid ? this : null;
			}
		}

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult)
		{
			var transformed = transformationResult.Transformed;
			// Start and end hash of an empty region will always be on the same token
			var directives = transformed.DescendantTokens()
				.Where(o => o.ContainsDirectives)
				.Select(o => o.Span)
				.OrderByDescending(o => o.Start)
				.ToList();
			foreach (var span in directives)
			{
				var token = transformed.DescendantTokens().First(o => o.Span == span);
				var toRemoveRegions = new List<RegionInfo>();
				var currentRegion = new RegionInfo();
				var stack = new Stack<RegionInfo>();

				for (var i = 0; i < token.LeadingTrivia.Count; i++)
				{
					var trivia = token.LeadingTrivia[i];
					// End of line trivia can be also located on the trailing trivia of a parent token in this case check for the whitespace
					if (trivia.IsKind(SyntaxKind.EndOfLineTrivia) || (!currentRegion.EndOfLineIndex.HasValue && trivia.IsKind(SyntaxKind.WhitespaceTrivia)))
					{
						currentRegion.EndOfLineIndex = i;
					}
					else if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
					{
						if (currentRegion.StartRegionIndex.HasValue) // Nested regions
						{
							stack.Push(currentRegion);
							currentRegion = new RegionInfo
							{
								Parent = currentRegion
							};
						}
						currentRegion.StartRegionIndex = i;
						currentRegion.StartRegionEndOfLineIndex = currentRegion.EndOfLineIndex;
					}
					else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
					{
						if (currentRegion.StartRegionIndex.HasValue)
						{
							currentRegion.EndIndex = i;
							toRemoveRegions.Add(currentRegion);
						}
						currentRegion = stack.Any() ? stack.Pop() : new RegionInfo();
					}
				}
				if (!toRemoveRegions.Any())
				{
					continue;
				}

				var newToken = token;
				foreach (var regionInfo in toRemoveRegions.Select(o => o.GetValidRoot()).Where(o => o != null).Distinct().OrderByDescending(o => o.StartIndex))
				{
					for (var j = regionInfo.EndIndex.GetValueOrDefault(); j >= regionInfo.StartIndex.GetValueOrDefault(); j--)
					{
						newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.RemoveAt(j));
					}
				}
				transformed = transformed.ReplaceToken(token, newToken);
			}
			return transformed;
		}
	}
}
