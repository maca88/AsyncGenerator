using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class BodyFunctionReferenceData : AbstractFunctionReferenceData, IBodyFunctionReferenceAnalyzationResult, IFunctionReferenceAnalyzation
	{
		public BodyFunctionReferenceData(FunctionData functionData, ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
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

		/// <summary>
		/// Functions passed as arguments
		/// </summary>
		public List<FunctionArgumentData> FunctionArguments { get; set; }

		public void AddFunctionArgument(FunctionArgumentData functionArgument)
		{
			FunctionArguments = FunctionArguments ?? new List<FunctionArgumentData>();
			FunctionArguments.Add(functionArgument);
		}

		public override ReferenceConversion GetConversion()
		{
			if (Ignore)
			{
				return ReferenceConversion.Ignore;
			}
			var conversion = ReferenceAsyncSymbols?.Count > 0 || ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
			if (conversion == ReferenceConversion.Ignore || FunctionArguments == null || !FunctionArguments.Any())
			{
				return conversion;
			}
			// If there is any function passed as an argument we have to check if they can all be async
			if (!FunctionArguments.All(o => o.AsyncCounterparts?.Count > 0 ||
			                                o.FunctionData?.Conversion == MethodConversion.ToAsync))
			{
				return ReferenceConversion.Ignore;
			}
			return ReferenceConversion.ToAsync;
		}

		#region IFunctionReferenceAnalyzationResult

		private IReadOnlyList<IMethodSymbol> _cachedReferenceAsyncSymbols;
		IReadOnlyList<IMethodSymbol> IBodyFunctionReferenceAnalyzationResult.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		bool IBodyFunctionReferenceAnalyzationResult.AwaitInvocation => AwaitInvocation.GetValueOrDefault();

		#endregion

		#region IFunctionReferenceAnalyzation

		IFunctionAnalyzationResult IFunctionReferenceAnalyzation.ReferenceFunctionData => ReferenceFunctionData;
		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzation.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		public override string AsyncCounterpartName { get; set; }

		public override IMethodSymbol AsyncCounterpartSymbol { get; set; }

		public override FunctionData AsyncCounterpartFunction { get; set; }

		#endregion
	}
}
