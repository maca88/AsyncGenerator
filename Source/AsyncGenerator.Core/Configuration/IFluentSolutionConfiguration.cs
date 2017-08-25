using System;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentSolutionConfiguration
	{
		IFluentSolutionConfiguration ConfigureProject(string projectName, Action<IFluentProjectConfiguration> action);

		/// <summary>
		/// Set if the analyzation and transformation steps for all configured projects will run concurrently by using <c>Parallel.ForEach</c> and <c>Task.WhenAll</c>.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentSolutionConfiguration ConcurrentRun(bool value = true);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		IFluentSolutionConfiguration ApplyChanges(bool value);

		/// <summary>
		/// Add one or more regex patterns to suppress failures that occur while opening the solution. (e.g. failure for opening a not supported project inside a solution)
		/// </summary>
		IFluentSolutionConfiguration SuppressDiagnosticFailures(params string[] patterns);

		/// <summary>
		/// Add a predicate to suppress failures that occur while opening the solution. (e.g. failure for opening a not supported project inside a solution)
		/// </summary>
		IFluentSolutionConfiguration SuppressDiagnosticFailures(Predicate<string> predicate);
	}
}
