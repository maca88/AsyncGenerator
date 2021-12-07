﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static AsyncGenerator.Core.Extensions.Internal.SyntaxNodeHelper;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Prepends a <see cref="OperationCanceledException"/> catch block
	/// </summary>
	internal class OperationCanceledExceptionFunctionRewriter : FunctionRewriter
	{
		private readonly SyntaxTrivia _eolTrivia;
		private readonly INamespaceTransformationMetadata _namespaceMetadata;

		private static readonly HashSet<string> IncludeExceptions = new HashSet<string>
		{
			$"{nameof(SystemException)}",
			$"{nameof(System)}.{nameof(SystemException)}",

			$"{nameof(Exception)}",
			$"{nameof(System)}.{nameof(Exception)}",
		};

		private static readonly HashSet<string> SkipExceptions = new HashSet<string>
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
			// Do not add an OperationCanceledException catch block if:
			// - there are no catch blocks (only finally)
			// - there already exists an OperationCanceledException catch block
			// - there are no ancestors of OperationCanceledException being catched
			// - there are no async calls that have a cancellation token argument and there are no guards added
			if (node.Catches.Count == 0 ||
			    node.Catches.Any(o => o.Declaration != null && SkipExceptions.Contains(o.Declaration.Type.ToString())) ||
			    node.Catches.All(o => o.Declaration != null && !IncludeExceptions.Contains(o.Declaration.Type.ToString())) ||
			    !node.Block.DescendantNodes(o => !o.IsFunction()).Any(o => o.HasAnnotations(Annotations.AsyncCallWithTokenOrGuard)))
			{
				return base.VisitTryStatement(node);
			}

			var catchClause = CatchClause()
				.WithCatchKeyword(
					Token(TriviaList(node.GetLeadingTrivia().LastOrDefault(o => o.IsKind(SyntaxKind.WhitespaceTrivia))), SyntaxKind.CatchKeyword, TriviaList(Space)))
				.WithDeclaration(
					CatchDeclaration(_namespaceMetadata.UsingSystem
							? IdentifierName(nameof(OperationCanceledException))
							: ConstructNameSyntax($"System.{nameof(OperationCanceledException)}"))
						.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(Space))))
				.WithBlock(
					Block(SingletonList<StatementSyntax>(
						ThrowStatement().WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(Space)))
					))
					.WithOpenBraceToken(Token(TriviaList(), SyntaxKind.OpenBraceToken, TriviaList(Space)))
					.WithCloseBraceToken(Token(TriviaList(), SyntaxKind.CloseBraceToken, TriviaList(_eolTrivia)))
				);
			node = node.WithCatches(new SyntaxList<CatchClauseSyntax>(new[] {catchClause}.Concat(node.Catches)));
			return base.VisitTryStatement(node);
		}
	}
}
