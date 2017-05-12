using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration
{
	public interface IProjectAnalyzeConfiguration
	{
		bool ScanMethodBody { get; }

		Predicate<INamedTypeSymbol> ScanForMissingAsyncMembers { get; }

		bool UseCancellationTokens { get; }

		IProjectCancellationTokenConfiguration CancellationTokens { get; }
	}
}
