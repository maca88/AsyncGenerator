using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Plugins;

namespace AsyncGenerator.Configuration
{
	public interface IFluentProjectConfiguration
	{
		IFluentProjectConfiguration ConfigureAnalyzation(Action<IFluentProjectAnalyzeConfiguration> action);

		IFluentProjectConfiguration ConfigureTransformation(Action<IFluentProjectTransformConfiguration> action);

		IFluentProjectConfiguration ConfigureCompilation(string outputPath, Action<IFluentProjectCompileConfiguration> action);

		IFluentProjectConfiguration RegisterPlugin<TPlugin>() where TPlugin : class, IPlugin, new();

		IFluentProjectConfiguration RegisterPlugin(IPlugin plugin);
	}
}
