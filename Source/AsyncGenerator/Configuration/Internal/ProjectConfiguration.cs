using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectConfiguration : IFluentProjectConfiguration, IProjectConfiguration
	{
		private readonly ISolutionConfiguration _solutionConfiguration;
		private bool? _applyChanges;
		private bool? _concurrentRun;

		public ProjectConfiguration(ISolutionConfiguration solutionConfiguration, string name) : this()
		{
			_solutionConfiguration = solutionConfiguration;
			Name = name;
		}

		public ProjectConfiguration(string path) : this()
		{
			Path = path;
		}

		private ProjectConfiguration()
		{
			ParseConfiguration = new ProjectParseConfiguration();
			AnalyzeConfiguration = new ProjectAnalyzeConfiguration(this);
			TransformConfiguration = new ProjectTransformConfiguration(this);
			RegisteredPlugins = new List<IPlugin>();
		}

		public bool ApplyChanges => _applyChanges ?? (_solutionConfiguration?.ApplyChanges ?? false);

		public bool ConcurrentRun => _concurrentRun ?? (_solutionConfiguration?.ConcurrentRun ?? false);

		public string Path { get; }

		public string Name { get; }

		public ProjectParseConfiguration ParseConfiguration { get; }

		public ProjectAnalyzeConfiguration AnalyzeConfiguration { get; }

		public ProjectTransformConfiguration TransformConfiguration { get; }

		public ProjectCompileConfiguration CompileConfiguration { get; private set; }

		public List<IPlugin> RegisteredPlugins { get; }

		public ImmutableArray<Predicate<string>> SuppressDiagnosticFailuresPrediactes { get; private set; } = ImmutableArray<Predicate<string>>.Empty;

		#region IProjectConfiguration

		IProjectAnalyzeConfiguration IProjectConfiguration.AnalyzeConfiguration => AnalyzeConfiguration;

		IProjectTransformConfiguration IProjectConfiguration.TransformConfiguration => TransformConfiguration;

		IProjectCompileConfiguration IProjectConfiguration.CompileConfiguration => CompileConfiguration;

		#endregion

		#region IFluentProjectConfiguration

		IFluentProjectConfiguration IFluentProjectConfiguration.ConcurrentRun(bool value)
		{
			_concurrentRun = value;
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.ApplyChanges(bool value)
		{
			_applyChanges = value;
			return this;
		}

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

		IFluentProjectConfiguration IFluentProjectConfiguration.SuppressDiagnosticFailures(params string[] patterns)
		{
			if (patterns == null)
			{
				throw new ArgumentNullException(nameof(patterns));
			}
			if (_solutionConfiguration != null)
			{
				throw new InvalidOperationException(
					"SuppressDiagnosticFailures cannot be called for a project that is defined inside a solution, use SuppressDiagnosticFailures from the solution instead.");
			}
			foreach (var pattern in patterns)
			{
				try
				{
					var regex = new Regex(pattern);
					SuppressDiagnosticFailuresPrediactes = SuppressDiagnosticFailuresPrediactes.Add(o => regex.IsMatch(o));
				}
				catch (Exception)
				{
					throw new InvalidOperationException($"Invalid regex pattern: '{pattern}'");
				}
			}
			return this;
		}

		IFluentProjectConfiguration IFluentProjectConfiguration.SuppressDiagnosticFailures(Predicate<string> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			if (_solutionConfiguration != null)
			{
				throw new InvalidOperationException(
					"SuppressDiagnosticFailures cannot be called for a project that is defined inside a solution, use SuppressDiagnosticFailures from the solution instead.");
			}
			SuppressDiagnosticFailuresPrediactes = SuppressDiagnosticFailuresPrediactes.Add(predicate);
			return this;
		}

		#endregion

		private void RegisterPlugin(IPlugin plugin)
		{
			TryAdd(plugin, AnalyzeConfiguration.FindAsyncCounterpartsFinders);
			TryAdd(plugin, AnalyzeConfiguration.InvocationExpressionAnalyzers);
			TryAdd(plugin, AnalyzeConfiguration.PreconditionCheckers);
			TryAdd(plugin, TransformConfiguration.DocumentTransformers);
			TryAdd(plugin, TransformConfiguration.FunctionReferenceTransformers);
			TryAdd(plugin, TransformConfiguration.MethodTransformers);
			TryAdd(plugin, TransformConfiguration.TypeTransformers);
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
