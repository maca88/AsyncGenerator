using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectExceptionHandlingConfiguration
	{
		Predicate<IMethodSymbol> CatchPropertyGetterCalls { get; }

		Func<IMethodSymbol, bool?> CatchFunctionBody { get; }
	}
}
