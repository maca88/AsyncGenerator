using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Plugins
{
	public interface IAsyncCounterpartsFinder
	{
		IEnumerable<IMethodSymbol> FindAsyncCounterparts(Project project, IMethodSymbol syncMethodSymbol, bool equalParameters bool );
	}
}
