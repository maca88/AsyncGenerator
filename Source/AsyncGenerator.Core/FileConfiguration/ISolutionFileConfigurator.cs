using System;
using System.Reflection;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Core.FileConfiguration
{
	public interface ISolutionFileConfigurator
	{
		AsyncGenerator Parse(string content);

		string GetSolutionPath(AsyncGenerator configuration);

		void Configure(AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration, Func<string, Assembly> codeCompiler);
	}
}
