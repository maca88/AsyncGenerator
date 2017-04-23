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
	/// <summary>
	/// Contains information of a function/method that is used inside a function/method body (eg. invoked, passed as an argument, assigned to a variable or event).
	/// </summary>
	public interface IBodyFunctionReferenceAnalyzationResult: IFunctionReferenceAnalyzationResult
	{
		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		bool Ignore { get; }

		bool AwaitInvocation { get; }

		bool CancellationTokenRequired { get; }

		ExpressionSyntax ConfigureAwaitParameter { get; }

		bool SynchronouslyAwaited { get; }

		bool UseAsReturnValue { get; }

		bool LastInvocation { get; }
	}
}
