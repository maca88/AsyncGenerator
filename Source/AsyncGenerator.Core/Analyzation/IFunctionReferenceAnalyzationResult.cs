using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IFunctionReferenceAnalyzationResult : IReferenceAnalyzationResult<IMethodSymbol>
	{
		IFunctionAnalyzationResult ReferenceFunction { get; }

		SyntaxNode ReferenceNode { get; }

		ReferenceConversion GetConversion();

		AsyncReturnType GetAsyncReturnType();

		string AsyncCounterpartName { get; }

		IMethodSymbol AsyncCounterpartSymbol { get; }

		IFunctionAnalyzationResult AsyncCounterpartFunction { get; }

		bool InsideMethodBody { get; }
	}
}
