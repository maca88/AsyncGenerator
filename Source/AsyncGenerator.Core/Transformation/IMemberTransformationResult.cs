using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	public interface IMemberTransformationResult : ITransformationResult
	{
		IMemberAnalyzationResult GetAnalyzationResult();

		SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		SyntaxTrivia EndOfLineTrivia { get; set; }

		SyntaxTrivia IndentTrivia { get; set; }
	}
}
