using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IChildFunctionAnalyzationResult : IFunctionAnalyzationResult
	{
		IFunctionAnalyzationResult ParentFunction { get; }
	}
}
