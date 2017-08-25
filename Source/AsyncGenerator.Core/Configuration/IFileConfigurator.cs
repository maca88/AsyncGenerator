using System;
using System.Reflection;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFileConfigurator
	{
		FileConfiguration.AsyncGenerator Parse(string content);

		void Configure(FileConfiguration.AsyncGenerator configuration, FileConfiguration.Solution solution, IFluentSolutionConfiguration solutionConfiguration, Assembly assembly);

		void Configure(FileConfiguration.AsyncGenerator configuration, FileConfiguration.Project project, IFluentProjectConfiguration projectConfiguration, Assembly assembly);
	}
}
