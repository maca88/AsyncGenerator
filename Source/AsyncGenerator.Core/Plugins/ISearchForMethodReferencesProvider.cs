using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface ISearchForMethodReferencesProvider : IPlugin
	{
		bool? SearchForMethodReferences(IMethodSymbol methodSymbol);
	}
}
