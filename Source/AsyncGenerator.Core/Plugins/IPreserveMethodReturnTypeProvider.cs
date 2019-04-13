using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IPreserveMethodReturnTypeProvider : IPlugin
	{
		bool? PreserveReturnType(IMethodSymbol methodSymbol);
	}
}
