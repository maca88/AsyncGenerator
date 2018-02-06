using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectExceptionHandlingConfiguration : IFluentProjectExceptionHandlingConfiguration
	{
		public Predicate<IMethodSymbol> CatchPropertyGetterCalls { get; private set; } = m => false;

		IFluentProjectExceptionHandlingConfiguration IFluentProjectExceptionHandlingConfiguration.CatchPropertyGetterCalls(Predicate<IMethodSymbol> predicate)
		{
			CatchPropertyGetterCalls = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}
	}
}
