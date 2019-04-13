using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface ITypeConversionProvider : IPlugin
	{
		TypeConversion? GetConversion(INamedTypeSymbol typeSymbol);
	}
}
