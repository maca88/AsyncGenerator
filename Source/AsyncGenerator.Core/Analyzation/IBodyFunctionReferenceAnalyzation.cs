using System.Collections.Generic;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Core.Analyzation
{
	// TODO: define what we should do with this interface
	/// <summary>
	/// Used by <see cref="IInvocationExpressionAnalyzer"/> in order to customize the analyzation process of a reference
	/// </summary>
	public interface IBodyFunctionReferenceAnalyzation : IBodyFunctionReferenceAnalyzationResult
	{
		IFunctionAnalyzationResult ReferenceFunctionData { get; }

		void Ignore(string reason);
	}
}
