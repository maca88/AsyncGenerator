using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation
{
	public interface IMemberTransformationResult : ITransformationResult
	{
		IMemberAnalyzationResult GetAnalyzationResult();

		SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		SyntaxTrivia EndOfLineTrivia { get; set; }

		SyntaxTrivia IndentTrivia { get; set; }
	}
}
