using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public class SolutionConfiguration : ISolutionConfiguration
	{
		public SolutionConfiguration(string path)
		{
			Path = path;
		}

		public List<ProjectConfiguration> ProjectConfigurations { get; } = new List<ProjectConfiguration>();

		public string Path { get; }

		public bool ApplyChanges { get; private set; }

		#region ISolutionConfiguration

		ISolutionConfiguration ISolutionConfiguration.ConfigureProject(string projectName, Action<IProjectConfiguration> action)
		{
			if (projectName == null)
			{
				throw new ArgumentNullException(nameof(projectName));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			var projectConfig = new ProjectConfiguration(projectName);
			ProjectConfigurations.Add(projectConfig);
			action(projectConfig);
			return this;
		}

		ISolutionConfiguration ISolutionConfiguration.ApplyChanges(bool value)
		{
			ApplyChanges = value;
			return this;
		}

		#endregion
	}
}
