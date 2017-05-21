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
		/// Set if the analyzation and transformation steps for all configured projects will run in parallel by using <c>Parallel.ForEach</c> and <c>Task.WhenAll</c>.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentSolutionConfiguration RunInParallel(bool value = true);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		IFluentSolutionConfiguration ApplyChanges(bool value);
	}
}
