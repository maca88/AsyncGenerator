using System;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectPreprocessorDirectivesConfiguration : IFluentProjectPreprocessorDirectivesConfiguration, IProjectPreprocessorDirectivesConfiguration
	{
		public Func<IMethodSymbol, PreprocessorDirectives> AddForMethod { get; private set; }

		IFluentProjectPreprocessorDirectivesConfiguration IFluentProjectPreprocessorDirectivesConfiguration.AddForMethod(Func<IMethodSymbol, PreprocessorDirectives> func)
		{
			AddForMethod = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}
	}
}
