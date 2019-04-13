using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectAnalyzeConfiguration
	{
		bool SearchAsyncCounterpartsInInheritedTypes { get; }

		bool ScanMethodBody { get; }

		Predicate<INamedTypeSymbol> CanScanForMissingAsyncMembers { get; }

		bool UseCancellationTokens { get; }

		IProjectCancellationTokenConfiguration CancellationTokens { get; }

		bool ConcurrentRun { get; }

		IProjectExceptionHandlingConfiguration ExceptionHandling { get; }

		Predicate<IMethodSymbol> CanAlwaysAwait { get; }
	}
}
