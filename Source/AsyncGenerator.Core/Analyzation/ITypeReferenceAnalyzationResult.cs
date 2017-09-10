using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface ITypeReferenceAnalyzationResult : IReferenceAnalyzationResult<INamedTypeSymbol>
	{
		ITypeAnalyzationResult TypeAnalyzationResult { get; }
	}
}
