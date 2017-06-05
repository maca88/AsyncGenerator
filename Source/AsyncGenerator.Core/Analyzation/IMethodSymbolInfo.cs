using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
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
