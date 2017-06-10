using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Core.Transformation
{
	public interface INamespaceTransformationResult : IMemberTransformationResult
	{
		INamespaceAnalyzationResult AnalyzationResult { get; }

		/// <summary>
		/// All transformed types inside this namespace
		/// </summary>
		IReadOnlyList<ITypeTransformationResult> TransformedTypes { get; }
	}
}
