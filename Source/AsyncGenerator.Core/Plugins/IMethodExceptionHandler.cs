using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IMethodExceptionHandler : IPlugin
	{
		bool? CatchMethodBody(IMethodSymbol methodSymbol, IMethodSymbol argumentOfMethodSymbol);
	}
}
