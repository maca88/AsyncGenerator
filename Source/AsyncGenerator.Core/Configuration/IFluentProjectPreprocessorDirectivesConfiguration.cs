using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectPreprocessorDirectivesConfiguration
	{
		/// <summary>
		/// Set a function that will add preprocessor directives before and after a method.
		/// </summary>
		/// <param name="func">A function that returns the preprocessor directives for the given method.</param>
		IFluentProjectPreprocessorDirectivesConfiguration AddForMethod(Func<IMethodSymbol, PreprocessorDirectives> func);
	}
}