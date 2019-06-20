using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Removes trivias based on the given predicate
	/// </summary>
	internal class TriviaRemover : CSharpSyntaxRewriter
	{
		private readonly Predicate<SyntaxNode> _descendIntoChildren;
		private readonly Predicate<SyntaxTrivia> _removeTrivia;

		public TriviaRemover(Predicate<SyntaxTrivia> removeTrivia) : this(removeTrivia, null)
		{
		}

		public TriviaRemover(
			Predicate<SyntaxTrivia> removeTrivia,
			Predicate<SyntaxNode> descendIntoChildren)
		{
			_removeTrivia = removeTrivia;
			_descendIntoChildren = descendIntoChildren;
		}

		public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
		{
			if (_removeTrivia(trivia))
			{
				return default(SyntaxTrivia);
			}
			return base.VisitTrivia(trivia);
		}

		public override SyntaxNode Visit(SyntaxNode node)
		{
			if (_descendIntoChildren != null && !_descendIntoChildren(node))
			{
				return node;
			}

			return base.Visit(node);
		}
	}
}
