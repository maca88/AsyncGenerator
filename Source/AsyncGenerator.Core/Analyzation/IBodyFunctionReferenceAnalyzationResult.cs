using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	/// <summary>
	/// Contains information of a function/method that is used inside a function/method body (eg. invoked, passed as an argument, assigned to a variable or event).
	/// </summary>
	public interface IBodyFunctionReferenceAnalyzationResult: IFunctionReferenceAnalyzationResult
	{
		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		bool AwaitInvocation { get; }

		bool PassCancellationToken { get; }

		ExpressionSyntax ConfigureAwaitParameter { get; }

		bool SynchronouslyAwaited { get; }

		bool UseAsReturnValue { get; }

		bool WrapInsideFunction { get; }

		bool LastInvocation { get; }

		IMethodSymbol AsyncDelegateArgument { get; }
	}
}
