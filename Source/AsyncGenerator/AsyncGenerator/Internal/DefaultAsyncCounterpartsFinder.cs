using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using static AsyncGenerator.Analyzation.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Internal
{
	internal class DefaultAsyncCounterpartsFinder : AbstractPlugin, IAsyncCounterpartsFinder
	{
		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, AsyncCounterpartsSearchOptions options)
		{
			return syncMethodSymbol.GetAsyncCounterparts(options.HasFlag(EqualParameters), options.HasFlag(SearchInheritTypes), 
				options.HasFlag(HasCancellationToken), options.HasFlag(IgnoreReturnType));
		}
	}
}
