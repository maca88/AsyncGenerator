using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Transformation
{
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
