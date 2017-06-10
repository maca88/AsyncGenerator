using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	public interface IProjectTransformationResult
	{
		Project Project { get; }

		IReadOnlyList<IDocumentTransformationResult> Documents { get; }
	}
}
