using System.Collections.Immutable;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	/// <summary>
	/// Holds the current information about the type that is under transformation process
	/// </summary>
	public interface ITypeTransformationMetadata
	{
		ITypeAnalyzationResult AnalyzationResult { get; }

		IImmutableSet<string> MemberNames { get; }

		SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		SyntaxTrivia EndOfLineTrivia { get; set; }

		SyntaxTrivia IndentTrivia { get; set; }
	}
}
