using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IFieldConversionProvider : IPlugin
	{
		FieldVariableConversion? GetFieldConversion(ISymbol fieldSymbol);
	}
}
