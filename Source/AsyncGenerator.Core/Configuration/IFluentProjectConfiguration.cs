using System;
using AsyncGenerator.Core.Plugins;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectConfiguration
	{
		/// <summary>
		/// Set if the analyzation and transformation steps for all configured projects will run concurrently by using <c>Parallel.ForEach</c> and <c>Task.WhenAll</c>.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectConfiguration ConcurrentRun(bool value = true);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		IFluentProjectConfiguration ApplyChanges(bool value);

		IFluentProjectConfiguration ConfigureParsing(Action<IFluentProjectParseConfiguration> action);

		IFluentProjectConfiguration ConfigureAnalyzation(Action<IFluentProjectAnalyzeConfiguration> action);

		IFluentProjectConfiguration ConfigureTransformation(Action<IFluentProjectTransformConfiguration> action);

		IFluentProjectConfiguration ConfigureCompilation(string outputPath);

		IFluentProjectConfiguration ConfigureCompilation(string outputPath, Action<IFluentProjectCompileConfiguration> action);

		IFluentProjectConfiguration RegisterPlugin<TPlugin>() where TPlugin : class, IPlugin, new();

		IFluentProjectConfiguration RegisterPlugin(IPlugin plugin);

		/// <summary>
		/// Add one or more regex patterns to suppress failures that occur while opening the project. Can be used only when configuring a project without a solution.
		/// </summary>
		IFluentProjectConfiguration SuppressDiagnosticFailures(params string[] patterns);

		/// <summary>
		/// Add a predicate to suppress failures that occur while opening the project. Can be used only when configuring a project without a solution.
		/// </summary>
		IFluentProjectConfiguration SuppressDiagnosticFailures(Predicate<string> predicate);
	}
}
