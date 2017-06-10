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

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult)
		{
			var transformed = transformationResult.Transformed;
			// Start and end hash of an empty region will always be on the same token
			foreach (var span in transformed.DescendantTokens().Where(o => o.ContainsDirectives).Select(o => o.Span).OrderByDescending(o => o.Start))
			{
				var token = transformed.DescendantTokens().First(o => o.Span == span);
				var startRegionIdx = -1;
				var startRegionEndOfLineIdx = -1;
				var currentEndOfLineIndex = -1;
				var toRemoveIndexes = new Dictionary<int, int>();
				for (var i = 0; i < token.LeadingTrivia.Count; i++)
				{
					var trivia = token.LeadingTrivia[i];
					// End of line trivia can be also located on the trailing trivia of a parent token in this case check for the whitespace
					if (trivia.IsKind(SyntaxKind.EndOfLineTrivia) || (currentEndOfLineIndex < 0 && trivia.IsKind(SyntaxKind.WhitespaceTrivia)))
					{
						currentEndOfLineIndex = i;
					}
					else if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
					{
						startRegionIdx = i;
						startRegionEndOfLineIdx = currentEndOfLineIndex;
					}
					else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
					{
						if (startRegionIdx < 0)
						{
							startRegionEndOfLineIdx = currentEndOfLineIndex = - 1;
							continue;
						}
						var removeFromIndex = startRegionEndOfLineIdx < 0 ? startRegionIdx : startRegionEndOfLineIdx;
						toRemoveIndexes.Add(removeFromIndex, i);
						startRegionIdx = currentEndOfLineIndex = - 1;
					}
				}
				if (!toRemoveIndexes.Any())
				{
					continue;
				}

				var newToken = token;
				foreach (var pair in toRemoveIndexes.OrderByDescending(o => o.Key))
				{
					var from = pair.Key;
					var to = pair.Value;
					for (var j = to; j >= from; j--)
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
