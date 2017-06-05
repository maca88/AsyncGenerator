using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Core.Transformation
{
	public interface ITypeTransformationResult : IMemberTransformationResult
	{
		ITypeAnalyzationResult AnalyzationResult { get; }

		/// <summary>
		/// All transformed methods inside this type
		/// </summary>
		IReadOnlyList<IMethodTransformationResult> TransformedMethods { get; }
	}
}
