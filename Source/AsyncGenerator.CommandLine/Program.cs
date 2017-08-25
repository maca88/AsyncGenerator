using System;
using System.IO;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core.Configuration;
using log4net.Config;
using Microsoft.VisualStudio.Setup.Configuration;

namespace AsyncGenerator.CommandLine
{
	static class Program
	{
		public static int Main(string[] args)
		{
			XmlConfigurator.Configure();
			Console.WriteLine("AsyncGenerator");
			Console.WriteLine();
			try
			{
				ConfigureMSBuild();
				var configuration = Configure();
				if (configuration == null)
				{
					Console.WriteLine("Could not find configuration file:");
					Console.WriteLine("\t - please make sure that your working directory contains either AsyncGenerator.xml or AsyncGenerator.yml");
					Console.WriteLine();
					Console.WriteLine("Working directory:");
					Console.WriteLine("\t- {0}", Environment.CurrentDirectory);
					Console.WriteLine();
					return 1;
				}

				var generator = new AsyncCodeGenerator();
				generator.GenerateAsync(configuration)
					.GetAwaiter()
					.GetResult();

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return -1;
			}
		}

		// Copied from here: https://github.com/T4MVC/R4MVC/commit/ae2fd5d8f3ab60708419d37c8a42d237d86d3061#diff-89dd7d1659695edb3702bfe879b34b09R61
		// in order to fix the issue https://github.com/Microsoft/msbuild/issues/2369 -> https://github.com/Microsoft/msbuild/issues/2030
		private static void ConfigureMSBuild()
		{
			var query = new SetupConfiguration();
			var query2 = (ISetupConfiguration2)query;

			try
			{
				if (query2.GetInstanceForCurrentProcess() is ISetupInstance2 instance)
				{
					Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
					Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
					return;
				}
			}
			catch { }

			var instances = new ISetupInstance[1];
			var e = query2.EnumAllInstances();
			int fetched;
			do
			{
				e.Next(1, instances, out fetched);
				if (fetched > 0)
				{
					var instance = instances[0] as ISetupInstance2;
					if (instance.GetInstallationVersion().StartsWith("15."))
					{
						Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
						Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
						return;
					}
				}
			}
			while (fetched > 0);
		}

		static AsyncCodeConfiguration Configure()
		{
			var xmlConfig = new FileInfo("AsyncGenerator.xml");
			if (xmlConfig.Exists)
			{
				using (var stream = xmlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.ConfigureFromStream<XmlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}
			var ymlConfig = new FileInfo("AsyncGenerator.yml");
			if (ymlConfig.Exists)
			{
				using (var stream = ymlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.ConfigureFromStream<YamlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}

			return null;
		}
	}
}
