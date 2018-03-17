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
	public interface IMethodOrAccessorTransformationResult : IMemberTransformationResult, IFunctionTransformationResult
	{
		/// <summary>
		/// The transformed method
		/// </summary>
		new MethodDeclarationSyntax Transformed { get; }

		new IMethodOrAccessorAnalyzationResult AnalyzationResult { get; }

		IReadOnlyList<ILockTransformationResult> TransformedLocks { get; }
	}
}
