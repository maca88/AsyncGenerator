using System;
using System.Collections.Generic;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectConfiguration : IFluentProjectConfiguration, IProjectConfiguration
	{
		public ProjectConfiguration(string name)
		{
			Name = name;
			ParseConfiguration = new ProjectParseConfiguration();
			AnalyzeConfiguration = new ProjectAnalyzeConfiguration();
			TransformConfiguration = new ProjectTransformConfiguration();
			RegisteredPlugins = new List<IPlugin>();
		}

		public string Name { get; }

		public ProjectParseConfiguration ParseConfiguration { get; }

		public ProjectAnalyzeConfiguration AnalyzeConfiguration { get; }

		public ProjectTransformConfiguration TransformConfiguration { get; }

		public ProjectCompileConfiguration CompileConfiguration { get; private set; }

		public List<IPlugin> RegisteredPlugins { get; }

		#region IProjectConfiguration

		IProjectAnalyzeConfiguration IProjectConfiguration.AnalyzeConfiguration => AnalyzeConfiguration;

		IProjectTransformConfiguration IProjectConfiguration.TransformConfiguration => TransformConfiguration;

		IProjectCompileConfiguration IProjectConfiguration.CompileConfiguration => CompileConfiguration;

		#endregion

		#region IFluentProjectConfiguration

		IFluentProjectConfiguration IFluentProjectConfiguration.ConfigureParsing(Action<IFluentProjectParseConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(ParseConfiguration);
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.ConfigureAnalyzation(Action<IFluentProjectAnalyzeConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(AnalyzeConfiguration);
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.ConfigureTransformation(Action<IFluentProjectTransformConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(TransformConfiguration);
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.ConfigureCompilation(string outputPath)
		{
			if (outputPath == null)
			{
				throw new ArgumentNullException(nameof(outputPath));
			}
			CompileConfiguration = new ProjectCompileConfiguration(outputPath);
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.ConfigureCompilation(string outputPath, Action<IFluentProjectCompileConfiguration> action)
		{
			if (outputPath == null)
			{
				throw new ArgumentNullException(nameof(outputPath));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			CompileConfiguration = new ProjectCompileConfiguration(outputPath);
			action(CompileConfiguration);
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.RegisterPlugin<TPlugin>()
		{
			RegisterPlugin(Activator.CreateInstance<TPlugin>());
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.RegisterPlugin(IPlugin plugin)
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
			TryAdd(plugin, TransformConfiguration.DocumentTransformers);
			TryAdd(plugin, TransformConfiguration.MethodTransformers);
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
