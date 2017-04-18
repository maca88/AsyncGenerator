using System;
using System.Collections.Generic;
using AsyncGenerator.Plugins;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectConfiguration : IProjectConfiguration
	{
		public ProjectConfiguration(string name)
		{
			Name = name;
			AnalyzeConfiguration = new ProjectAnalyzeConfiguration();
			TransformConfiguration = new ProjectTransformConfiguration();
			RegisteredPlugins = new List<IPlugin>();
		}

		public string Name { get; }

		public ProjectAnalyzeConfiguration AnalyzeConfiguration { get; }

		public ProjectTransformConfiguration TransformConfiguration { get; }

		public ProjectCompileConfiguration CompileConfiguration { get; private set; }

		public List<IPlugin> RegisteredPlugins { get; }

		#region IProjectConfiguration

		IProjectConfiguration IProjectConfiguration.ConfigureAnalyzation(Action<IProjectAnalyzeConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(AnalyzeConfiguration);
			return this;
		}

		IProjectConfiguration IProjectConfiguration.ConfigureTransformation(Action<IProjectTransformConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(TransformConfiguration);
			return this;
		}

		IProjectConfiguration IProjectConfiguration.ConfigureCompilation(string outputPath, Action<IProjectCompileConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			CompileConfiguration = new ProjectCompileConfiguration(outputPath);
			action(CompileConfiguration);
			return this;
		}

		IProjectConfiguration IProjectConfiguration.RegisterPlugin<TPlugin>()
		{
			RegisterPlugin(Activator.CreateInstance<TPlugin>());
			return this;
		}

		IProjectConfiguration IProjectConfiguration.RegisterPlugin(IPlugin plugin)
		{
			if (plugin == null)
			{
				throw new ArgumentNullException(nameof(plugin));
			}
			RegisterPlugin(plugin);
			return this;
		}

		#endregion

		private void RegisterPlugin(IPlugin plugin)
		{
			TryAdd(plugin, AnalyzeConfiguration.FindAsyncCounterpartsFinders);
			TryAdd(plugin, AnalyzeConfiguration.PreconditionCheckers);
			TryAdd(plugin, AnalyzeConfiguration.InvocationExpressionAnalyzers);
			RegisteredPlugins.Add(plugin);
		}

		private void TryAdd<T>(IPlugin plugin, ICollection<T> list) where T : class
		{
			var obj = plugin as T;
			if (obj != null)
			{
				list.Add(obj);
			}
		}

	}
}
