using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	public interface IFunctionTransformationResult : ITransformationResult, IFunctionTransformationTrivia
	{
		/// <summary>
		/// The transformed function
		/// </summary>
		SyntaxNode Transformed { get; }

		IFunctionAnalyzationResult AnalyzationResult { get; }

		IReadOnlyList<IFunctionReferenceTransformationResult> TransformedFunctionReferences { get; }
	}
}
