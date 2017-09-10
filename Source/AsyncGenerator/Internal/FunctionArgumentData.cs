using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal class FunctionArgumentData
	{
		public FunctionArgumentData(FunctionData functionData, int index)
		{
			Index = index;
			FunctionData = functionData;
		}

		public FunctionArgumentData(BodyReferenceFunctionData functionReference, int index)
		{
			FunctionReference = functionReference;
			Index = index;
		}

		public BodyReferenceFunctionData FunctionReference { get; }

		public FunctionData FunctionData { get; }

		public int Index { get; }
	}
}
