using System;
using System.IO;
using System.Threading;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core.Configuration;
using log4net;
using log4net.Config;

namespace AsyncGenerator.CommandLine
{
	static class Program
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

		public static int Main(string[] args)
		{
			//XmlConfigurator.Configure();
			Logger.Info($"AsyncGenerator{Environment.NewLine}");
			var cancellationSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, e) =>
			{
				Logger.Warn("Canceling operations...");
				cancellationSource.Cancel();
			};
			try
			{
				ConfigureMSBuild();
				var configuration = Configure();
				if (configuration == null)
				{
					Logger.Info("Could not find configuration file:");
					Logger.Info($"\t - please make sure that your working directory contains either AsyncGenerator.xml or AsyncGenerator.yml{Environment.NewLine}");
					Logger.Info("Working directory:");
					Logger.Info($"\t- {Environment.CurrentDirectory}{Environment.NewLine}");
					return 1;
				}

				var generator = new AsyncCodeGenerator();
				generator.GenerateAsync(configuration, cancellationSource.Token)
					.GetAwaiter()
					.GetResult();

				return 0;
			}
			catch (Exception e)
			{
				Logger.Fatal(e);
				return -1;
			}
		}

		// Copied from here: https://github.com/T4MVC/R4MVC/commit/ae2fd5d8f3ab60708419d37c8a42d237d86d3061#diff-89dd7d1659695edb3702bfe879b34b09R61
		// in order to fix the issue https://github.com/Microsoft/msbuild/issues/2369 -> https://github.com/Microsoft/msbuild/issues/2030
		private static void ConfigureMSBuild()
		{
#if NETCORE2
			Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH",
				@"C:\Program Files\dotnet\sdk\2.0.0\MSBuild.dll"
				//@"C:\Workspace\Git\AsyncGenerator\Source\AsyncGenerator.Tests\bin\Debug\netcoreapp2.0\MSBuild.dll"
			);
			// Needed in order to debug
			Environment.SetEnvironmentVariable("VsInstallRoot", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community");
#endif
#if NET461
				Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community");
				Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
#endif

				//var query = new SetupConfiguration();
				//var query2 = (ISetupConfiguration2)query;

				//try
				//{
				//	if (query2.GetInstanceForCurrentProcess() is ISetupInstance2 instance)
				//	{
				//		Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
				//		Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
				//		return;
				//	}
				//}
				//catch { }

				//var instances = new ISetupInstance[1];
				//var e = query2.EnumAllInstances();
				//int fetched;
				//do
				//{
				//	e.Next(1, instances, out fetched);
				//	if (fetched > 0)
				//	{
				//		var instance = instances[0] as ISetupInstance2;
				//		if (instance.GetInstallationVersion().StartsWith("15."))
				//		{
				//			Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
				//			Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
				//			return;
				//		}
				//	}
				//}
				//while (fetched > 0);
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
