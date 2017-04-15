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
	public enum FunctionReferenceConversion
	{
		Ignore,
		ToAsync
	}

	public class FunctionReferenceData : IFunctionReferenceAnalyzationResult, IFunctionReferenceAnalyzation
	{
		public FunctionReferenceData(FunctionData functionData, ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
		{
			FunctionData = functionData;
			ReferenceLocation = reference;
			ReferenceNameNode = referenceNameNode;
			ReferenceSymbol = referenceSymbol;
			ReferenceFunctionData = referenceFunctionData;
		}

		public FunctionData FunctionData { get; }

		public FunctionData ReferenceFunctionData { get; }

		public SimpleNameSyntax ReferenceNameNode { get; }

		public SyntaxNode ReferenceNode { get; internal set; }

		public ReferenceLocation ReferenceLocation { get; }

		public IMethodSymbol ReferenceSymbol { get; }

		public HashSet<IMethodSymbol> ReferenceAsyncSymbols { get; set; }

		public bool Ignore { get; set; }

		public bool? AwaitInvocation { get; internal set; }

		public ExpressionSyntax ConfigureAwaitParameter { get; set; }

		public bool SynchronouslyAwaited { get; set; }

		public bool CancellationTokenRequired { get; set; }

		public bool UsedAsReturnValue { get; internal set; }

		public bool LastInvocation { get; internal set; }

		public FunctionReferenceConversion GetConversion()
		{
			return !Ignore &&
			       (ReferenceAsyncSymbols?.Count > 0 || ReferenceFunctionData?.Conversion == MethodConversion.ToAsync)
				? FunctionReferenceConversion.ToAsync
				: FunctionReferenceConversion.Ignore;
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

		private IReadOnlyList<IMethodSymbol> _cachedReferenceAsyncSymbols;
		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzationResult.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		bool IFunctionReferenceAnalyzationResult.AwaitInvocation => AwaitInvocation.GetValueOrDefault();

		#endregion

		#region IFunctionReferenceAnalyzation

		IFunctionAnalyzationResult IFunctionReferenceAnalyzation.ReferenceFunctionData => ReferenceFunctionData;
		IReadOnlyList<IMethodSymbol> IFunctionReferenceAnalyzation.ReferenceAsyncSymbols => 
			_cachedReferenceAsyncSymbols ?? (_cachedReferenceAsyncSymbols = ReferenceAsyncSymbols.ToImmutableArray());

		#endregion
	}
}
