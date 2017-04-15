using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	public interface IFunctionReferenceAnalyzationResult
	{
		IFunctionAnalyzationResult ReferenceFunctionData { get; }

		SimpleNameSyntax ReferenceNameNode { get; }

		ReferenceLocation ReferenceLocation { get; }

		IMethodSymbol ReferenceSymbol { get; }

		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		SyntaxNode ReferenceNode { get; }

		FunctionReferenceConversion GetConversion();

		bool Ignore { get; }

		bool AwaitInvocation { get; }

		bool CancellationTokenRequired { get; }

		ExpressionSyntax ConfigureAwaitParameter { get; }

		bool SynchronouslyAwaited { get; }

		bool UsedAsReturnValue { get; }

		bool LastInvocation { get; }
	}
}
