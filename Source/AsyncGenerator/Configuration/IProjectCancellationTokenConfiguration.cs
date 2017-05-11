using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration
{
	public interface IProjectCancellationTokenConfiguration
	{
		bool Guards { get; }

		Func<IMethodSymbolInfo, MethodCancellationToken> MethodGeneration { get; }

		Func<IMethodSymbol, bool?> RequireCancellationToken { get; }
	}
}
