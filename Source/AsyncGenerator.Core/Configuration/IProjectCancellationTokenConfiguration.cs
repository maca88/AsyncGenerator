using System;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectCancellationTokenConfiguration
	{
		bool Guards { get; }

		Func<IMethodSymbolInfo, MethodCancellationToken> MethodGeneration { get; }

		Func<IMethodSymbol, bool?> RequiresCancellationToken { get; }
	}
}
