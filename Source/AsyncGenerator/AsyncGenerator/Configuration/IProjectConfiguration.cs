using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Plugins;

namespace AsyncGenerator.Configuration
{
	public interface IProjectConfiguration
	{
		IProjectConfiguration ConfigureAnalyzation(Action<IProjectAnalyzeConfiguration> action);

		IProjectConfiguration ConfigureTransformation(Action<IProjectTransformConfiguration> action);

		IProjectConfiguration ConfigureCompilation(string outputPath, Action<IProjectCompileConfiguration> action);

		IProjectConfiguration RegisterPlugin<TPlugin>() where TPlugin : class, IPlugin, new();

		IProjectConfiguration RegisterPlugin(IPlugin plugin);
	}
}
