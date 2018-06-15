using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using AsyncGenerator.Plugins.Internal;
using Microsoft.CodeAnalysis;
using static AsyncGenerator.Core.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Internal
{
	internal class DefaultAsyncCounterpartsFinder : AbstractPlugin, IAsyncCounterpartsFinder
	{
		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			return syncMethodSymbol.GetAsyncCounterparts(invokedFromType, options.HasFlag(EqualParameters), options.HasFlag(SearchInheritedTypes), 
				options.HasFlag(HasCancellationToken), options.HasFlag(IgnoreReturnType));
		}
	}
}
