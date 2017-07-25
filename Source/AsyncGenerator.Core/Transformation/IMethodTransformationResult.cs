using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Transformation
{
	public interface IMethodTransformationResult : IMethodOrAccessorTransformationResult
	{
		new IMethodAnalyzationResult AnalyzationResult { get; }

	}

	public interface IAccessorTransformationResult : IMethodOrAccessorTransformationResult
	{
		new IAccessorAnalyzationResult AnalyzationResult { get; }
	}

	public interface IMethodOrAccessorTransformationResult : IMemberTransformationResult
	{
		/// <summary>
		/// The transformed method
		/// </summary>
		MethodDeclarationSyntax Transformed { get; }

		IMethodOrAccessorAnalyzationResult AnalyzationResult { get; }

		SyntaxTrivia BodyLeadingWhitespaceTrivia { get; }

		IReadOnlyList<IFunctionReferenceTransformationResult> TransformedFunctionReferences { get; }

		IReadOnlyList<ILockTransformationResult> TransformedLocks { get; }
	}
}
