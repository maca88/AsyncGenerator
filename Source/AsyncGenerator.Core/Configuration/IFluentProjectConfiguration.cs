using System;
using AsyncGenerator.Core.Plugins;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectConfiguration
	{
		IFluentProjectConfiguration ConfigureParsing(Action<IFluentProjectParseConfiguration> action);

		IFluentProjectConfiguration ConfigureAnalyzation(Action<IFluentProjectAnalyzeConfiguration> action);

		IFluentProjectConfiguration ConfigureTransformation(Action<IFluentProjectTransformConfiguration> action);

		IFluentProjectConfiguration ConfigureCompilation(string outputPath);

		IFluentProjectConfiguration ConfigureCompilation(string outputPath, Action<IFluentProjectCompileConfiguration> action);

		IFluentProjectConfiguration RegisterPlugin<TPlugin>() where TPlugin : class, IPlugin, new();

		IFluentProjectConfiguration RegisterPlugin(IPlugin plugin);
	}
}
