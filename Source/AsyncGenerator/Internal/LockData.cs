using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class LockData : ILockAnalyzationResult
	{
		public LockData(ISymbol symbol, LockStatementSyntax node)
		{
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public ISymbol Symbol { get; }

		public LockStatementSyntax Node { get; }
	}
}
