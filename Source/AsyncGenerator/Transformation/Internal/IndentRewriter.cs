using System;
using System.Linq;
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
		private SyntaxToken? _currentToken;

		public IndentRewriter(string indent, bool subtract = false)
		{
			_indent = indent;
			_subtract = subtract;
		}

		public override SyntaxToken VisitToken(SyntaxToken token)
		{
			if (token.IsKind(SyntaxKind.None))
			{
				return token;
			}

			token = NeedsIndent() ? token.WithLeadingTrivia(AlterIndent(token.LeadingTrivia)) : token;
			_currentToken = base.VisitToken(token);
			return _currentToken.Value;
		}

		public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
		{
			if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
			{
				var root = (CompilationUnitSyntax)ParseSyntaxTree($"void Method() {{\n{trivia.ToFullString()}\n}}").GetRoot();
				var body = (BlockSyntax)Visit(root.Members.OfType<MethodDeclarationSyntax>().First().Body);
				return DisabledText(string.Join("", body.Statements.Select(o => o.ToFullString())));
			}

			return base.VisitTrivia(trivia);
		}

		private bool NeedsIndent()
		{
			return !_currentToken.HasValue ||
			       _currentToken.Value.TrailingTrivia.Any(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
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
						: Whitespace(_indent + trivia));
			}
			return leadTrivia;
		}
	}
}
