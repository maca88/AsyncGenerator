using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation
{
	public interface IProjectTransformationResult
	{
		Project Project { get; }

		IReadOnlyList<IDocumentTransformationResult> Documents { get; }
	}
}
