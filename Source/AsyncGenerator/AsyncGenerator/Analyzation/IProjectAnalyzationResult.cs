using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Analyzation
{
	public interface IProjectAnalyzationResult
	{
		Project Project { get; }

		IReadOnlyList<IDocumentAnalyzationResult> Documents { get; }
	}
}
