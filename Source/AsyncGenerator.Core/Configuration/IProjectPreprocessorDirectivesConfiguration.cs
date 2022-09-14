using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectPreprocessorDirectivesConfiguration
	{
		Func<IMethodSymbol, PreprocessorDirectives> AddForMethod { get; }
	}
}