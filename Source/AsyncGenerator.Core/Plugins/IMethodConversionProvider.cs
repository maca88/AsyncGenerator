using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IMethodConversionProvider : IPlugin
	{
		MethodConversion? GetConversion(IMethodSymbol methodSymbol);
	}
}
