using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal abstract class ChildFunctionData : FunctionData, IChildFunctionAnalyzationResult
	{
		protected ChildFunctionData(IMethodSymbol methodSymbol, FunctionData parentFunction)
			: base(methodSymbol)
		{
			ParentFunction = parentFunction;
		}

		public FunctionData ParentFunction { get; }

		#region IChildFunctionAnalyzationResult

		IFunctionAnalyzationResult IChildFunctionAnalyzationResult.ParentFunction => ParentFunction;

		#endregion
	}
}
