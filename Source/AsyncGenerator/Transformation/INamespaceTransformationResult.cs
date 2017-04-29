using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Transformation
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
