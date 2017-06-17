using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface ILockAnalyzationResult
	{
		/// <summary>
		/// Symbol of the lock statement expression
		/// </summary>
		ISymbol Symbol { get; }

		/// <summary>
		/// Lock statement node
		/// </summary>
		LockStatementSyntax Node { get; }
	}
}
