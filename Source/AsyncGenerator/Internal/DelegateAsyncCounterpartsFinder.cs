using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Plugins;
using AsyncGenerator.Plugins.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class DelegateAsyncCounterpartsFinder : AbstractPlugin, IAsyncCounterpartsFinder
	{
		private readonly Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> _func;

		public DelegateAsyncCounterpartsFinder(Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func)
		{
			_func = func;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, AsyncCounterpartsSearchOptions options)
		{
			return _func(syncMethodSymbol, options);
		}
	}
}
