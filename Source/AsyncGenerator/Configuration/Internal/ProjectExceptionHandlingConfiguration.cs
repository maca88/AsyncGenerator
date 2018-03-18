using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectExceptionHandlingConfiguration : IFluentProjectExceptionHandlingConfiguration, IProjectExceptionHandlingConfiguration
	{
		public Predicate<IMethodSymbol> CatchPropertyGetterCalls { get; private set; } = m => false;

		public Func<IMethodSymbol, bool?> CatchFunctionBody { get; private set; }

		IFluentProjectExceptionHandlingConfiguration IFluentProjectExceptionHandlingConfiguration.CatchPropertyGetterCalls(Predicate<IMethodSymbol> predicate)
		{
			CatchPropertyGetterCalls = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		public IFluentProjectExceptionHandlingConfiguration CatchMethodBody(Func<IMethodSymbol, bool?> func)
		{
			CatchFunctionBody = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}
	}
}
