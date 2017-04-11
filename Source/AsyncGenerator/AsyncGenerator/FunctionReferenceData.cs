using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public enum FunctionReferenceDataConversion
	{
		Ignore,
		ToAsync
	}

	public class FunctionReferenceData : IFunctionReferenceAnalyzationResult, IFunctionReferenceAnalyzation
	{
		public FunctionReferenceData(FunctionData functionData, ReferenceLocation reference, SimpleNameSyntax referenceNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
		{
			FunctionData = functionData;
			ReferenceLocation = reference;
			ReferenceNode = referenceNode;
			ReferenceSymbol = referenceSymbol;
			ReferenceFunctionData = referenceFunctionData;
		}

		public FunctionData FunctionData { get; }

		public FunctionData ReferenceFunctionData { get; }

		public SimpleNameSyntax ReferenceNode { get; }

		public SyntaxKind ReferenceKind { get; internal set; }

		public ReferenceLocation ReferenceLocation { get; }

		public IMethodSymbol ReferenceSymbol { get; }

		public HashSet<IMethodSymbol> ReferenceAsyncSymbols { get; set; }

		public bool CanBeAsync { get; set; }

		public bool AwaitInvocation { get; internal set; } = true;

		public ExpressionSyntax ConfigureAwaitParameter { get; set; }

		public bool SynchronouslyAwaited { get; set; }

		public bool CancellationTokenRequired { get; set; }

		// Replaced by ReferenceKind
		//public bool PassedAsArgument { get; internal set; }

		public bool UsedAsReturnValue { get; internal set; }

		public FunctionReferenceDataConversion GetConversion()
		{
			return CanBeAsync &&
			       (ReferenceAsyncSymbols?.Count > 0 || ReferenceFunctionData?.Conversion == MethodConversion.ToAsync)
				? FunctionReferenceDataConversion.ToAsync
				: FunctionReferenceDataConversion.Ignore;
		}

		public override int GetHashCode()
		{
			return ReferenceLocation.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != GetType())
			{
				return false;
			}
			return ReferenceLocation.Equals(((FunctionReferenceData)obj).ReferenceLocation);
		}

		#region IFunctionReferenceAnalyzationResult

		IFunctionAnalyzationResult IFunctionReferenceAnalyzationResult.ReferenceFunctionData => ReferenceFunctionData;

		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzationResult.ReferenceAsyncSymbols => ReferenceAsyncSymbols.ToImmutableArray();

		#endregion

		#region IFunctionReferenceAnalyzation

		IFunctionAnalyzationResult IFunctionReferenceAnalyzation.ReferenceFunctionData => ReferenceFunctionData;

		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzation.ReferenceAsyncSymbols => ReferenceAsyncSymbols.ToImmutableArray();

		#endregion
	}
}
