using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions.Internal;
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

		public override ReferenceConversion Conversion { get; set; }

		public string IgnoredReason { get; private set; }

		public bool? AwaitInvocation { get; internal set; }

		public ExpressionSyntax ConfigureAwaitParameter { get; set; }

		public bool SynchronouslyAwaited { get; set; }

		public ITypeSymbol InvokedFromType { get; set; }

		public bool PassCancellationToken { get; set; }

		public IParameterSymbol CancellationTokenParameter { get; set; }

		public bool UseAsReturnValue { get; internal set; }

		public bool WrapInsideFunction { get; internal set; }

		public bool LastInvocation { get; internal set; }

		public IMethodSymbol AsyncDelegateArgument { get; set; }

		public BodyFunctionReferenceData ArgumentOfFunctionInvocation { get; set; }

		/// <summary>
		/// Functions passed as arguments
		/// </summary>
		public List<FunctionArgumentData> FunctionArguments { get; set; }

		public void AddFunctionArgument(FunctionArgumentData functionArgument)
		{
			FunctionArguments = FunctionArguments ?? new List<FunctionArgumentData>();
			FunctionArguments.Add(functionArgument);
		}

		public void Ignore(string reason)
		{
			Conversion = ReferenceConversion.Ignore;
			IgnoredReason = reason;
			if (FunctionArguments == null)
			{
				return;
			}
			foreach (var functionArgument in FunctionArguments)
			{
				functionArgument.FunctionData?.Ignore("Cascade ignored.");
				functionArgument.FunctionReference?.Ignore("Cascade ignored.");
			}
		}

		public override ReferenceConversion GetConversion()
		{
			if (Conversion != ReferenceConversion.Unknown)
			{
				return Conversion;
			}
			var conversion = ReferenceFunctionData?.Conversion.HasFlag(MethodConversion.ToAsync) == true
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
			if (conversion == ReferenceConversion.Ignore || FunctionArguments == null || !FunctionArguments.Any())
			{
				return conversion;
			}
			// If there is any function passed as an argument we have to check if they can all be async
			if (!FunctionArguments.All(o => o.FunctionReference?.GetConversion() == ReferenceConversion.ToAsync ||
			                                o.FunctionData?.Conversion.HasFlag(MethodConversion.ToAsync) == true))
			{
				return ReferenceConversion.Ignore;
			}
			return ReferenceConversion.ToAsync;
		}

		public bool? CanSkipCancellationTokenArgument()
		{
			if (ReferenceFunctionData != null)
			{
				var refMethodData = ReferenceFunctionData.GetMethodData();
					return refMethodData.MethodCancellationToken.GetValueOrDefault().HasOptionalCancellationToken();
			}

			if (ReferenceAsyncSymbols == null || !ReferenceAsyncSymbols.Any())
			{
				return null;
			}
			foreach (var referenceAsyncSymbol in ReferenceAsyncSymbols)
			{
				if (referenceAsyncSymbol.Parameters.Length == ReferenceSymbol.Parameters.Length)
				{
					return true;
				}
				if (referenceAsyncSymbol.Parameters.Length > ReferenceSymbol.Parameters.Length && referenceAsyncSymbol.Parameters.Last().IsOptional)
				{
					return true;
				}
			}
			return false;
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
