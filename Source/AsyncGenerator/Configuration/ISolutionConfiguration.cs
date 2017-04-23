using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface ISolutionConfiguration
	{
		ISolutionConfiguration ConfigureProject(string projectName, Action<IProjectConfiguration> action);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		ISolutionConfiguration ApplyChanges(bool value);
	}
}
