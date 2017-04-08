using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Extensions
{
	public static class SyntaxNodeExtensions
	{
		/// <summary>
		/// Check if the statement is a precondition. A statement will qualify for a precondition only if it is a 
		/// <see cref="IfStatementSyntax"/> and contains a <see cref="ThrowExpressionSyntax"/>
		/// </summary>
		/// <param name="statement">The statement to be checked</param>
		/// <returns></returns>
		public static bool IsPrecondition(this StatementSyntax statement)
		{
			var ifStatement = statement as IfStatementSyntax;
			if (ifStatement?.Statement == null)
			{
				return false;
			}
			// A statement can be a ThrowStatement or a Block that contains a ThrowStatement
			if (!ifStatement.Statement.IsKind(SyntaxKind.Block))
			{
				return ifStatement.Statement.IsKind(SyntaxKind.ThrowStatement);
			}
			var blockStatements = ifStatement.Statement.DescendantNodes().OfType<StatementSyntax>().ToList();
			return blockStatements.Count == 1 && blockStatements[0].IsKind(SyntaxKind.ThrowStatement);
		}

	}
}
