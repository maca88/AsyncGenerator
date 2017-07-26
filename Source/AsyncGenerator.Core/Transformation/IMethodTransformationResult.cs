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
}
