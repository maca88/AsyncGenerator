using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IAnonymousFunctionAnalyzationResult : IFunctionAnalyzationResult
	{
		AnonymousFunctionExpressionSyntax Node { get; }
	}
}
