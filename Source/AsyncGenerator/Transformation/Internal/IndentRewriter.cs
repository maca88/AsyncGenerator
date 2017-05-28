using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	public class IndentRewriter : CSharpSyntaxRewriter
	{
		private readonly string _indent;

		// TODO: unit test and add missing ones
		private readonly HashSet<SyntaxKind> _validSyntaxes = new HashSet<SyntaxKind>
		{
			SyntaxKind.CatchClause,
			SyntaxKind.FinallyClause,
			SyntaxKind.ElseClause,
			SyntaxKind.CaseSwitchLabel
		};

		public IndentRewriter(string indent)
		{
			_indent = indent;
		}

		public override SyntaxNode Visit(SyntaxNode node)
		{
			if (node == null || (!(node is StatementSyntax) && !_validSyntaxes.Contains(node.Kind())))
			{
				return base.Visit(node);
			}
			node = node.WithLeadingTrivia(AppendIndent(node.GetLeadingTrivia()));
			return base.Visit(node);
		}

		public override SyntaxNode VisitBlock(BlockSyntax node)
		{
			node = node
				.WithCloseBraceToken(
					node.CloseBraceToken.WithLeadingTrivia(AppendIndent(node.CloseBraceToken.LeadingTrivia))
				);
			return base.VisitBlock(node);
		}

		private SyntaxTriviaList AppendIndent(SyntaxTriviaList leadTrivia)
		{
			var whitespaces = leadTrivia
				.Select((trivia, i) => new
				{
					Trivia = trivia,
					Index = i
				})
				.Where(o => o.Trivia.IsKind(SyntaxKind.WhitespaceTrivia))
				.ToList();
			foreach (var whitespace in whitespaces)
			{
				leadTrivia = leadTrivia
					.RemoveAt(whitespace.Index)
					.Insert(whitespace.Index, Whitespace(whitespace.Trivia.ToFullString() + _indent));
			}
			return leadTrivia;
		}

	}
}
