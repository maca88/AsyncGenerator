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
			MethodSymbol = functionData.Symbol;
			Index = index;
			FunctionData = functionData;
		}

		public FunctionArgumentData(IMethodSymbol methodSymbol, int index, IEnumerable<IMethodSymbol> asyncCounterparts)
		{
			MethodSymbol = methodSymbol;
			Index = index;
			AsyncCounterparts = asyncCounterparts.ToImmutableArray();
		}

		public IMethodSymbol MethodSymbol { get; }

		public FunctionData FunctionData { get; }

		public IReadOnlyList<IMethodSymbol> AsyncCounterparts { get; }

		public int Index { get; }
	}
}
