using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Internal;

namespace AsyncGenerator.Configuration
{
	public class AsyncCodeConfiguration
	{
		private AsyncCodeConfiguration() {}

		public static AsyncCodeConfiguration Create()
		{
			return new AsyncCodeConfiguration();
		}

		internal List<SolutionConfiguration> SolutionConfigurations { get; } = new List<SolutionConfiguration>();

		internal List<ProjectConfiguration> ProjectConfigurations { get; } = new List<ProjectConfiguration>();

		public AsyncCodeConfiguration ConfigureSolution(string solutionFilePath, Action<IFluentSolutionConfiguration> action)
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
			var solutionConfig = SolutionConfigurations.FirstOrDefault(o => o.Path == solutionFilePath);
			if (solutionConfig == null)
			{
				solutionConfig = new SolutionConfiguration(solutionFilePath);
				SolutionConfigurations.Add(solutionConfig);
			}
			action(solutionConfig);
			return this;
		}

		public AsyncCodeConfiguration ConfigureProject(string projectFilePath, Action<IFluentProjectConfiguration> action)
		{
			if (projectFilePath == null)
			{
				throw new ArgumentNullException(nameof(projectFilePath));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			if (!File.Exists(projectFilePath))
			{
				throw new FileNotFoundException($"Project not found. Path:'{projectFilePath}'");
			}
			var solutionConfig = ProjectConfigurations.FirstOrDefault(o => o.Path == projectFilePath);
			if (solutionConfig == null)
			{
				solutionConfig = new ProjectConfiguration(projectFilePath);
				ProjectConfigurations.Add(solutionConfig);
			}
			action(solutionConfig);
			return this;
		}

		public AsyncCodeConfiguration ConfigureFromFile<T>(string filePath) where T : class, IFileConfigurator, new()
		{
			return ConfigureFromFile(filePath, new T());
		}

		public AsyncCodeConfiguration ConfigureFromStream<T>(Stream stream, string basePath = null) where T : class, IFileConfigurator, new()
		{
			return ConfigureFromStream(stream, new T(), basePath);
		}

		private AsyncCodeConfiguration ConfigureFromFile(string filePath, IFileConfigurator fileConfigurator)
		{
			if (filePath == null)
			{
				throw new ArgumentNullException(nameof(filePath));
			}

			filePath = Path.GetFullPath(Path.Combine(GetExecutingDirectory(), filePath));
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException($"Configuration file not found. Path:'{filePath}'");
			}
			return ConfigureFromStream(File.OpenRead(filePath), fileConfigurator, Path.GetDirectoryName(filePath));
		}

		private AsyncCodeConfiguration ConfigureFromStream(Stream stream, IFileConfigurator fileConfigurator, string basePath = null)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}
			if (fileConfigurator == null)
			{
				throw new ArgumentNullException(nameof(fileConfigurator));
			}

			basePath = basePath ?? GetExecutingDirectory();
			Core.FileConfiguration.AsyncGenerator configuration;
			using (var reader = new StreamReader(stream))
			{
				configuration = fileConfigurator.Parse(reader.ReadToEnd());
			}

			Assembly assembly = null;
			if (!string.IsNullOrEmpty(configuration.CSharpScript))
			{
				assembly = SourceCodeCompiler.Compile(configuration.CSharpScript);
			}

			if (!string.IsNullOrEmpty(configuration.Solution.FilePath))
			{
				var filePath = Path.GetFullPath(Path.Combine(basePath, configuration.Solution.FilePath));
				ConfigureSolution(filePath, o => fileConfigurator.Configure(configuration, configuration.Solution, o, assembly));
			}
			foreach (var project in configuration.Projects)
			{
				var filePath = Path.GetFullPath(Path.Combine(basePath, project.FilePath));
				ConfigureProject(filePath, o => fileConfigurator.Configure(configuration, project, o, assembly));
			}

			return this;
		}

		private static string GetExecutingDirectory()
		{
			var codeBase = Assembly.GetExecutingAssembly().CodeBase;
			var uri = new UriBuilder(codeBase);
			var assemblyPath = Uri.UnescapeDataString(uri.Path);
			var dir = Path.GetDirectoryName(assemblyPath);
			return dir;
		}
	}
}
