using System;
using System.Collections.Generic;

namespace AsyncGenerator.Configuration.Internal
{
	internal class SolutionConfiguration : IFluentSolutionConfiguration, ISolutionConfiguration
	{
		public SolutionConfiguration(string path)
		{
			Path = path;
		}

		public List<ProjectConfiguration> ProjectConfigurations { get; } = new List<ProjectConfiguration>();

		public string Path { get; }

		public bool ApplyChanges { get; private set; }

		public bool RunInParallel { get; private set; }

		#region IFluentSolutionConfiguration

		IFluentSolutionConfiguration IFluentSolutionConfiguration.ConfigureProject(string projectName, Action<IFluentProjectConfiguration> action)
		{
			if (projectName == null)
			{
				throw new ArgumentNullException(nameof(projectName));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			var projectConfig = new ProjectConfiguration(this, projectName);
			ProjectConfigurations.Add(projectConfig);
			action(projectConfig);
			return this;
		}

		IFluentSolutionConfiguration IFluentSolutionConfiguration.ApplyChanges(bool value)
		{
			ApplyChanges = value;
			return this;
		}

		IFluentSolutionConfiguration IFluentSolutionConfiguration.RunInParallel(bool value)
		{
			RunInParallel = value;
			return this;
		}

		#endregion
	}
}
