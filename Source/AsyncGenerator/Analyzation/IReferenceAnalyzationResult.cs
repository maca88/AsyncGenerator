using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	public interface IReferenceAnalyzationResult
	{
		SimpleNameSyntax ReferenceNameNode { get; }

		ReferenceLocation ReferenceLocation { get; }
	}

	public interface IReferenceAnalyzationResult<out TSymbol> : IReferenceAnalyzationResult where TSymbol : ISymbol
	{
		TSymbol ReferenceSymbol { get; }
	}
}
