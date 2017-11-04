using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IDelegateArgumentAnalyzationResult
	{
		/// <summary>
		/// Index of the delegate argument
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Function that represent the argument. Not null when the argument is an internal function.
		/// </summary>
		IFunctionAnalyzationResult Function { get; }

		/// <summary>
		/// Body function reference that represent the argument. Not null when the argument is an external function.
		/// </summary>
		IBodyFunctionReferenceAnalyzationResult BodyFunctionReference { get; }
	}
}
