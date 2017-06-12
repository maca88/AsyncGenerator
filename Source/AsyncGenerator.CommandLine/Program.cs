using System;
using System.IO;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.CommandLine
{
	static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("AsyncGenerator");
			Console.WriteLine();
			var configuration = Configure();
			if (configuration == null)
			{
				Console.WriteLine("Could not find configuration file:");
				Console.WriteLine("\t - please make sure that your working directory contains either AsyncGenerator.xml or AsyncGenerator.yml");
				Console.WriteLine();
				Console.WriteLine("Working directory:");
				Console.WriteLine("\t- {0}", Environment.CurrentDirectory);
				Console.WriteLine();
				return;
			}

			var generator = new AsyncCodeGenerator();
			
			generator.GenerateAsync(configuration)
				.GetAwaiter().GetResult();
		}

		static AsyncCodeConfiguration Configure()
		{
			var xmlConfig = new FileInfo("AsyncGenerator.xml");
			if (xmlConfig.Exists)
			{
				using (var stream = xmlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.ConfigureSolutionFromStream<XmlFileConfigurator>(stream);
				}
			}
			var ymlConfig = new FileInfo("AsyncGenerator.yml");
			if (ymlConfig.Exists)
			{
				using (var stream = ymlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.ConfigureSolutionFromStream<YamlFileConfigurator>(stream);
				}
			}

			return null;
		}
	}
}
