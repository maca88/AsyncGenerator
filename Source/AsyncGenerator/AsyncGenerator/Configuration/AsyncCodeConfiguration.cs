using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public interface ITransformPlugin
	{
		void Configure(Project project, ProjectTransformConfiguration configuration);
	}

	public interface IAnalyzePlugin
	{
		void Configure(Project project, ProjectAnalyzeConfiguration configuration);
	}

	public interface IProjectConfiguration
	{
		IProjectConfiguration ConfigureAnalyzation(Action<IProjectAnalyzeConfiguration> action);

		IProjectConfiguration ConfigureTransformation(Action<IProjectTransformConfiguration> action);

		IProjectConfiguration ConfigureCompilation(string outputPath, Action<IProjectCompileConfiguration> action);
	}

	public class ProjectConfiguration : IProjectConfiguration
	{
		public ProjectConfiguration(string name)
		{
			Name = name;
			AnalyzeConfiguration = new ProjectAnalyzeConfiguration();
			TransformConfiguration = new ProjectTransformConfiguration();
		}

		public string Name { get; }

		public ProjectAnalyzeConfiguration AnalyzeConfiguration { get; }

		public ProjectTransformConfiguration TransformConfiguration { get; }

		public ProjectCompileConfiguration CompileConfiguration { get; private set; }

		public IProjectConfiguration ConfigureAnalyzation(Action<IProjectAnalyzeConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(AnalyzeConfiguration);
			return this;
		}

		public IProjectConfiguration ConfigureTransformation(Action<IProjectTransformConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(TransformConfiguration);
			return this;
		}

		public IProjectConfiguration ConfigureCompilation(string outputPath, Action<IProjectCompileConfiguration> action)
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
	}

	public interface ISolutionConfiguration
	{
		ISolutionConfiguration ConfigureProject(string projectName, Action<IProjectConfiguration> action);

		/// <summary>
		/// Set if changes to projects and documents should be applied at the end of the transformation process
		/// </summary>
		ISolutionConfiguration ApplyChanges(bool value);
	}

	public class SolutionConfiguration : ISolutionConfiguration
	{
		public SolutionConfiguration(string path)
		{
			Path = path;
		}

		public List<ProjectConfiguration> ProjectConfigurations { get; } = new List<ProjectConfiguration>();

		public string Path { get; }

		public bool ApplyChanges { get; private set; }

		public ISolutionConfiguration ConfigureProject(string projectName, Action<IProjectConfiguration> action)
		{
			if (projectName == null)
			{
				throw new ArgumentNullException(nameof(projectName));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			var projectConfig = new ProjectConfiguration(projectName);
			ProjectConfigurations.Add(projectConfig);
			action(projectConfig);
			return this;
		}

		ISolutionConfiguration ISolutionConfiguration.ApplyChanges(bool value)
		{
			ApplyChanges = value;
			return this;
		}
	}


	public interface IAsyncCodeConfiguration
	{
		IAsyncCodeConfiguration ConfigureSolution(string solutionFilePath, Action<ISolutionConfiguration> action);

		AsyncCodeConfiguration Build();
	}

	public class AsyncCodeConfiguration : IAsyncCodeConfiguration
	{
		public static IAsyncCodeConfiguration Create()
		{
			return new AsyncCodeConfiguration();
		}

		public List<SolutionConfiguration> SolutionConfigurations { get; } = new List<SolutionConfiguration>();

		public IAsyncCodeConfiguration ConfigureSolution(string solutionFilePath, Action<ISolutionConfiguration> action)
		{
			if (solutionFilePath == null)
			{
				throw new ArgumentNullException(nameof(solutionFilePath));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			if (!File.Exists(solutionFilePath))
			{
				throw new FileNotFoundException($"Solution not found. Path:'{solutionFilePath}'");
			}
			var solutionConfig = new SolutionConfiguration(solutionFilePath);
			SolutionConfigurations.Add(solutionConfig);
			action(solutionConfig);
			return this;
		}

		public AsyncCodeConfiguration Build()
		{
			return this;
		}
	}
}
