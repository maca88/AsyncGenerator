using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Configuration.Internal
{
	internal class SolutionConfiguration : IFluentSolutionConfiguration, ISolutionConfiguration
	{
		public SolutionConfiguration(string path)
		{
			Path = path;
		}

		public List<ProjectConfiguration> ProjectConfigurations { get; } = new List<ProjectConfiguration>();

		public string Path { get; }

		public bool ApplyChanges { get; private set; }

		public bool ConcurrentRun { get; private set; }

		public ImmutableArray<Predicate<string>> SuppressDiagnosticFailuresPrediactes { get; private set; } = ImmutableArray<Predicate<string>>.Empty;

		#region IFluentSolutionConfiguration

		IFluentSolutionConfiguration IFluentSolutionConfiguration.ConfigureProject(string projectName, Action<IFluentProjectConfiguration> action)
		{
			if (projectName == null)
			{
				throw new ArgumentNullException(nameof(projectName));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			var projectConfig = new ProjectConfiguration(this, projectName);
			ProjectConfigurations.Add(projectConfig);
			action(projectConfig);
			return this;
		}

		IFluentSolutionConfiguration IFluentSolutionConfiguration.ApplyChanges(bool value)
		{
			ApplyChanges = value;
			return this;
		}

		IFluentSolutionConfiguration IFluentSolutionConfiguration.SuppressDiagnosticFailures(params string[] patterns)
		{
			if (patterns == null)
			{
				throw new ArgumentNullException(nameof(patterns));
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

		IFluentSolutionConfiguration IFluentSolutionConfiguration.SuppressDiagnosticFailures(Predicate<string> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			SuppressDiagnosticFailuresPrediactes = SuppressDiagnosticFailuresPrediactes.Add(predicate);
			return this;
		}

		IFluentSolutionConfiguration IFluentSolutionConfiguration.ConcurrentRun(bool value)
		{
			ConcurrentRun = value;
			return this;
		}

		#endregion
	}
}
