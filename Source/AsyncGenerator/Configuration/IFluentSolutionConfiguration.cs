using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface IFluentSolutionConfiguration
	{
		IFluentSolutionConfiguration ConfigureProject(string projectName, Action<IFluentProjectConfiguration> action);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		IFluentSolutionConfiguration ApplyChanges(bool value);
	}
}
