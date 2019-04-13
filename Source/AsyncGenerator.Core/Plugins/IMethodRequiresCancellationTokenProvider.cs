using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IMethodRequiresCancellationTokenProvider : IPlugin
	{
		bool? RequiresCancellationToken(IMethodSymbol methodSymbol);
	}
}
