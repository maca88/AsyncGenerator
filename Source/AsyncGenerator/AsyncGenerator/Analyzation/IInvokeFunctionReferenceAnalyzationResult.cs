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
	public interface IInvokeFunctionReferenceAnalyzationResult: IFunctionReferenceAnalyzationResult
	{
		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		bool Ignore { get; }

		bool AwaitInvocation { get; }

		bool CancellationTokenRequired { get; }

		ExpressionSyntax ConfigureAwaitParameter { get; }

		bool SynchronouslyAwaited { get; }

		bool UsedAsReturnValue { get; }

		bool LastInvocation { get; }
	}
}
