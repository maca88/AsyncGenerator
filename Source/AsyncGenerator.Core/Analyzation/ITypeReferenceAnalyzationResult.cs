using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface ITypeReferenceAnalyzationResult : IReferenceAnalyzationResult<INamedTypeSymbol>
	{
		bool IsCref { get; }

		ITypeAnalyzationResult TypeAnalyzationResult { get; }
	}
}
