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

		SimpleNameSyntax ReferenceNode { get; }

		ReferenceLocation ReferenceLocation { get; }

		IMethodSymbol ReferenceSymbol { get; }

		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		SyntaxKind ReferenceKind { get; }

		bool CanBeAsync { get; }

		bool AwaitInvocation { get; }

		bool CancellationTokenRequired { get; }

		ExpressionSyntax ConfigureAwaitParameter { get; }

		bool SynchronouslyAwaited { get; }

		bool UsedAsReturnValue { get; }

	}
}
