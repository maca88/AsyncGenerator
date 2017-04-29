using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Transformation
{
	public interface ITypeTransformationResult : ITransformationResult
	{
		ITypeAnalyzationResult AnalyzationResult { get; }

		/// <summary>
		/// All transformed methods inside this type
		/// </summary>
		IReadOnlyList<IMethodTransformationResult> TransformedMethods { get; }
	}
}
