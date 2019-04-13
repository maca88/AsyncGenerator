using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Configuration.Internal;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal class SolutionData
	{
		public SolutionData(Solution solution, SolutionConfiguration configuration)
		{
			Configuration = configuration;
			Solution = solution;
		}

		public readonly SolutionConfiguration Configuration;

		public Solution Solution { get; set; }

		internal Dictionary<ProjectId, ProjectData> ProjectData { get; } = new Dictionary<ProjectId, ProjectData>();

		/// <summary>
		/// Retrieve all projects in the same order that were configured
		/// </summary>
		/// <returns></returns>
		internal IEnumerable<ProjectData> GetProjects()
		{
			var projectDatas = ProjectData.Values.ToDictionary(o => o.Project.Name);
			return Configuration.ProjectConfigurations.Select(o => projectDatas[o.Name]);
		}

	}
}
