using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Transformation
{
	public interface IMethodTransformationResult : IMemberTransformationResult
	{
		IMethodAnalyzationResult AnalyzationResult { get; }

		/// <summary>
		/// The transformed method
		/// </summary>
		MethodDeclarationSyntax Transformed { get; }

		SyntaxTrivia BodyLeadingWhitespaceTrivia { get; }

		IReadOnlyList<IFunctionReferenceTransformationResult> TransformedFunctionReferences { get; }

		IReadOnlyList<ILockTransformationResult> TransformedLocks { get; }
	}
}
