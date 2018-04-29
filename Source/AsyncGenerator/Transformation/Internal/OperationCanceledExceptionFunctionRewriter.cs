using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = AsyncGenerator.Extensions.Internal.SyntaxNodeExtensions;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Prepends a <see cref="OperationCanceledException"/> catch block
	/// </summary>
	internal class OperationCanceledExceptionFunctionRewriter : FunctionRewriter
	{
		private readonly SyntaxTrivia _eolTrivia;
		private readonly INamespaceTransformationMetadata _namespaceMetadata;

		private static HashSet<string> _validExceptions = new HashSet<string>
		{
			$"{nameof(SystemException)}",
			$"{nameof(System)}.{nameof(SystemException)}",

			$"{nameof(Exception)}",
			$"{nameof(System)}.{nameof(Exception)}",
		};

		private static HashSet<string> _skipExceptions = new HashSet<string>
		{
			$"{nameof(OperationCanceledException)}",
			$"{nameof(System)}.{nameof(OperationCanceledException)}"
		};

		public OperationCanceledExceptionFunctionRewriter(SyntaxTrivia eolTrivia, INamespaceTransformationMetadata namespaceMetadata)
		{
			_eolTrivia = eolTrivia;
			_namespaceMetadata = namespaceMetadata;
		}

		public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
		{
			// Do not add a OperationCanceledException catch block if:
			// - there are no catch blocks (only finally)
			// - there already exists a OperationCanceledException catch block
			// - there are no ancestors of OperationCanceledException being catched
			if (node.Catches.Count == 0 ||
			    node.Catches.Any(o => o.Declaration != null &&
			                          (
				                          _skipExceptions.Contains(o.Declaration.Type.ToString()) ||
				                          !_validExceptions.Contains(o.Declaration.Type.ToString())
			                          )))
			{
				return base.VisitTryStatement(node);
			}

			var catchClause = CatchClause()
				.WithCatchKeyword(Token(TriviaList(node.GetLeadingTrivia()), SyntaxKind.CatchKeyword, TriviaList(Space)))
				.WithDeclaration(
					CatchDeclaration(_namespaceMetadata.UsingSystem
							? IdentifierName(nameof(OperationCanceledException))
							: SyntaxNodeExtensions.ConstructNameSyntax($"System.{nameof(OperationCanceledException)}"))
						.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(Space))))
				.WithBlock(
					Block(SingletonList<StatementSyntax>(
						ThrowStatement().WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(Space)))
					))
					.WithOpenBraceToken(Token(TriviaList(), SyntaxKind.OpenBraceToken, TriviaList(Space)))
					.WithCloseBraceToken(Token(TriviaList(), SyntaxKind.CloseBraceToken, TriviaList(_eolTrivia)))
				);
			node = node.WithCatches(new SyntaxList<CatchClauseSyntax>(new[] {catchClause}.Union(node.Catches)));
			return base.VisitTryStatement(node);
		}
	}
}
