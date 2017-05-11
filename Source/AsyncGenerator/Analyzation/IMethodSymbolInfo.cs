using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Analyzation
{
	public interface IMethodSymbolInfo
	{
		IMethodSymbol Symbol { get; }

		/// <summary>
		/// Interface members within project that the method implements
		/// </summary>
		IReadOnlyList<IMethodSymbol> ImplementedInterfaces { get; }

		/// <summary>
		/// Methods within project that the method overrides. Includes all overriden methods including the abstract/virtual one.
		/// </summary>
		IReadOnlyList<IMethodSymbol> OverridenMethods { get; }
	}
}
