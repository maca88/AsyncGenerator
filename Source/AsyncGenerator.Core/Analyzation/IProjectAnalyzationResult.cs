using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IProjectAnalyzationResult
	{
		Project Project { get; }

		IReadOnlyList<IDocumentAnalyzationResult> Documents { get; }
	}
}
