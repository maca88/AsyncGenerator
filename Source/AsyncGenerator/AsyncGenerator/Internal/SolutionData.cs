using System.Collections.Concurrent;
using AsyncGenerator.Configuration.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace AsyncGenerator.Internal
{
	internal class SolutionData
	{
		public SolutionData(Solution solution, MSBuildWorkspace buildWorkspace, SolutionConfiguration configuration)
		{
			Configuration = configuration;
			Workspace = buildWorkspace;
			Solution = solution;
		}

		public MSBuildWorkspace Workspace { get; }

		public readonly SolutionConfiguration Configuration;

		public Solution Solution { get; set; }

		internal ConcurrentDictionary<ProjectId, ProjectData> ProjectData { get; } = new ConcurrentDictionary<ProjectId, ProjectData>();

	}
}
