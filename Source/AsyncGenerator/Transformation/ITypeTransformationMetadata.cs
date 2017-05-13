using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation
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
