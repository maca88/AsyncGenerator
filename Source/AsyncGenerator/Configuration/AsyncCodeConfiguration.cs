using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.FileConfiguration;
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

		public AsyncCodeConfiguration ConfigureSolutionFromFile<T>(string filePath) where T : class, ISolutionFileConfigurator, new()
		{
			return ConfigureSolutionFromFile(filePath, new T());
		}

		public AsyncCodeConfiguration ConfigureSolutionFromStream<T>(Stream stream) where T : class, ISolutionFileConfigurator, new()
		{
			return ConfigureSolutionFromStream(stream, new T());
		}

		private AsyncCodeConfiguration ConfigureSolutionFromFile(string filePath, ISolutionFileConfigurator fileConfigurator)
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
			return ConfigureSolutionFromStream(File.OpenRead(filePath), fileConfigurator, Path.GetDirectoryName(filePath) + @"\");
		}

		private AsyncCodeConfiguration ConfigureSolutionFromStream(Stream stream, ISolutionFileConfigurator fileConfigurator, string basePath = null)
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
			var solutionFilePath = fileConfigurator.GetSolutionPath(configuration);

			solutionFilePath = Path.GetFullPath(Path.Combine(basePath, solutionFilePath));
			ConfigureSolution(solutionFilePath, o => fileConfigurator.Configure(configuration, o, SourceCodeCompiler.Compile));
			return this;
		}

		private static string GetExecutingDirectory()
		{
			var codeBase = Assembly.GetExecutingAssembly().CodeBase;
			var uri = new UriBuilder(codeBase);
			var assemblyPath = Uri.UnescapeDataString(uri.Path);
			var dir = Path.GetDirectoryName(assemblyPath);
			if (!dir.EndsWith(@"\"))
			{
				dir += @"\";
			}
			return dir;
		}
	}
}
