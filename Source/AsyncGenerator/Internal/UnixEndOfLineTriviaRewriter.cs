using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Internal
{
	/// <summary>
	/// Roslyn may add a CarriageReturnLineFeed when <see cref="Microsoft.CodeAnalysis.SyntaxNodeExtensions.RemoveNode{TRoot}"/> is called, even when running on Unix.
	/// This rewriter will replace all EndOfLine trivias that have CarriageReturnLineFeed with LineFeed
	/// </summary>
	internal class UnixEndOfLineTriviaRewriter : CSharpSyntaxRewriter
	{
		public override SyntaxToken VisitToken(SyntaxToken token)
		{
			token = base.VisitToken(token);
			if (token.HasLeadingTrivia)
			{
				token = token.WithLeadingTrivia(token.LeadingTrivia
					.Select(o => o.IsKind(SyntaxKind.EndOfLineTrivia) && o.ToFullString() == CarriageReturnLineFeed.ToFullString()
						? LineFeed
						: o));
			}
			return token;
		}
	}
}
