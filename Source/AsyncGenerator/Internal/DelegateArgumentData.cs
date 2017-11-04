using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Internal
{
	internal class DelegateArgumentData : IDelegateArgumentAnalyzationResult
	{
		public DelegateArgumentData(FunctionData functionData, int index)
		{
			Index = index;
			FunctionData = functionData;
		}

		public DelegateArgumentData(BodyFunctionDataReference functionReference, int index)
		{
			FunctionReference = functionReference;
			Index = index;
		}

		public BodyFunctionDataReference FunctionReference { get; }

		public FunctionData FunctionData { get; }

		public int Index { get; }

		IFunctionAnalyzationResult IDelegateArgumentAnalyzationResult.Function => FunctionData;

		IBodyFunctionReferenceAnalyzationResult IDelegateArgumentAnalyzationResult.BodyFunctionReference => FunctionReference;
	}
}
