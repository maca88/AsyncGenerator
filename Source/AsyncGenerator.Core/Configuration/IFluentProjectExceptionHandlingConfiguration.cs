using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectExceptionHandlingConfiguration
	{
		/// <summary>
		/// Set a predicate that will decide whether a property getter call shall be wrapped inside a try/catch block.
		/// Note that for auto-properties that are not virtual/abstract/interface the predicate won't be called.
		/// <para>Default false is chosen for all property getter calls.</para>
		/// </summary>
		IFluentProjectExceptionHandlingConfiguration CatchPropertyGetterCalls(Predicate<IMethodSymbol> predicate);

		///// <summary>
		///// Set a custom code that will be executed inside the catch block prior returning a faulted task.
		///// <para>Default no code is added.</para>
		///// </summary>
		//IFluentProjectExceptionHandlingConfiguration CustomCatchCode(string code);
	}
}
