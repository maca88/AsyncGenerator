using System.Collections.Generic;
using System.Collections.Immutable;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class InvokeFunctionReferenceData : AbstractFunctionReferenceData, IInvokeFunctionReferenceAnalyzationResult, IFunctionReferenceAnalyzation
	{
		public InvokeFunctionReferenceData(FunctionData functionData, ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
			: base(reference, referenceNameNode, referenceSymbol, referenceFunctionData)
		{
			FunctionData = functionData;
		}

		public FunctionData FunctionData { get; }

		public HashSet<IMethodSymbol> ReferenceAsyncSymbols { get; set; }

		public bool Ignore { get; set; }

		public bool? AwaitInvocation { get; internal set; }

		public ExpressionSyntax ConfigureAwaitParameter { get; set; }

		public bool SynchronouslyAwaited { get; set; }

		public bool CancellationTokenRequired { get; set; }

		public bool UseAsReturnValue { get; internal set; }

		public bool LastInvocation { get; internal set; }

		public override ReferenceConversion GetConversion()
		{
			return !Ignore &&
			       (ReferenceAsyncSymbols?.Count > 0 || ReferenceFunctionData?.Conversion == MethodConversion.ToAsync)
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
		}

		#region IFunctionReferenceAnalyzationResult

		private IReadOnlyList<IMethodSymbol> _cachedReferenceAsyncSymbols;
		IReadOnlyList<IMethodSymbol> IInvokeFunctionReferenceAnalyzationResult.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		bool IInvokeFunctionReferenceAnalyzationResult.AwaitInvocation => AwaitInvocation.GetValueOrDefault();

		#endregion

		#region IFunctionReferenceAnalyzation

		IFunctionAnalyzationResult IFunctionReferenceAnalyzation.ReferenceFunctionData => ReferenceFunctionData;
		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzation.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		#endregion
	}
}
