using System;
using System.Reflection;

namespace AsyncGenerator.Core.Configuration
{
	public interface ISolutionFileConfigurator
	{
		FileConfiguration.AsyncGenerator Parse(string content);

		string GetSolutionPath(FileConfiguration.AsyncGenerator configuration);

		void Configure(FileConfiguration.AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration, Func<string, Assembly> codeCompiler);
	}
}
