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
	public interface IFunctionReferenceAnalyzationResult : IReferenceAnalyzationResult<IMethodSymbol>
	{
		IFunctionAnalyzationResult ReferenceFunctionData { get; }

		SyntaxNode ReferenceNode { get; }

		ReferenceConversion GetConversion();

		string AsyncCounterpartName { get; }
	}
}
