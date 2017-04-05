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
	public interface IFunctionReferenceAnalyzationResult
	{
		IFunctionAnalyzationResult ReferenceFunctionData { get; }

		SimpleNameSyntax ReferenceNode { get; }

		ReferenceLocation ReferenceLocation { get; }

		IMethodSymbol ReferenceSymbol { get; }

		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		SyntaxKind ReferenceKind { get; }

		bool CanBeAsync { get; }

		bool CanBeAwaited { get; }
	}

	public class FunctionReferenceData : IFunctionReferenceAnalyzationResult
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

		public bool CanBeAwaited { get; internal set; } = true;

		// Replaced by ReferenceKind
		//public bool PassedAsArgument { get; internal set; }

		public bool UsedAsReturnValue { get; internal set; }

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
	}
}
