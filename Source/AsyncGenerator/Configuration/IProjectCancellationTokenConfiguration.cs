using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration
{
	public interface IProjectCancellationTokenConfiguration
	{
		bool Guards { get; }

		Func<IMethodSymbol, MethodCancellationToken> MethodGeneration { get; }

		Func<IMethodSymbol, bool?> RequireCancellationToken { get; }
	}
}
