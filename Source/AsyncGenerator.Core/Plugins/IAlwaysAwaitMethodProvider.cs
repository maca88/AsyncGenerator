using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IAlwaysAwaitMethodProvider : IPlugin
	{
		bool? AlwaysAwait(IMethodSymbol methodSymbol);
	}
}
