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
	internal class IndentRewriter : CSharpSyntaxRewriter
	{
		private readonly string _indent;
		private readonly bool _subtract;

		// TODO: unit test and add missing ones
		private readonly HashSet<SyntaxKind> _validSyntaxes = new HashSet<SyntaxKind>
		{
			SyntaxKind.CatchClause,
			SyntaxKind.FinallyClause,
			SyntaxKind.ElseClause,
			SyntaxKind.CaseSwitchLabel
		};

		public IndentRewriter(string indent, bool subtract = false)
		{
			_indent = indent;
			_subtract = subtract;
		}

		public override SyntaxNode Visit(SyntaxNode node)
		{
			if (node == null || (!(node is StatementSyntax) && !_validSyntaxes.Contains(node.Kind())))
			{
				return base.Visit(node);
			}
			node = node.WithLeadingTrivia(AlterIndent(node.GetLeadingTrivia()));
			return base.Visit(node);
		}

		public override SyntaxNode VisitBlock(BlockSyntax node)
		{
			node = node
				.WithCloseBraceToken(
					node.CloseBraceToken.WithLeadingTrivia(AlterIndent(node.CloseBraceToken.LeadingTrivia))
				);
			return base.VisitBlock(node);
		}

		private SyntaxTriviaList AlterIndent(SyntaxTriviaList leadTrivia)
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
				var trivia = whitespace.Trivia.ToFullString();
				leadTrivia = leadTrivia
					.RemoveAt(whitespace.Index)
					.Insert(whitespace.Index, _subtract
						? Whitespace(trivia.Substring(0, Math.Max(trivia.Length - _indent.Length, 0)))
						: Whitespace(trivia + _indent));
			}
			return leadTrivia;
		}

	}
}
