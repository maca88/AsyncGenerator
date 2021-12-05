using System;
using System.IO;
#if !NETCOREAPP
using System.Reflection;
using System.Text;
#endif
using System.Threading;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AsyncGenerator.CommandLine
{
	static class Program
	{
		public static int Main(string[] args)
		{
			var config = new ConfigurationBuilder()
				.AddJsonFile("logging.settings.json")
				.Build();
			var loggingFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConfiguration(config).AddConsole();
			});
			var logger = loggingFactory.CreateLogger($"{nameof(AsyncGenerator)}.{nameof(CommandLine)}");

			logger.LogInformation($"AsyncGenerator{Environment.NewLine} ({System.Reflection.Assembly.GetExecutingAssembly().Location})");
			var cancellationSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, e) =>
			{
				logger.LogWarning("Canceling operations...");
				cancellationSource.Cancel();
			};
			try
			{
				EnvironmentHelper.Setup();
				var configuration = Configure(loggingFactory);
				if (configuration == null)
				{
					logger.LogInformation("Could not find configuration file:");
					logger.LogInformation(
						$"\t - please make sure that your working directory contains either AsyncGenerator.xml or AsyncGenerator.yml{Environment.NewLine}");
					logger.LogInformation("Working directory:");
					logger.LogInformation($"\t- {Environment.CurrentDirectory}{Environment.NewLine}");
					return 1;
				}

				AsyncCodeGenerator.GenerateAsync(configuration, cancellationSource.Token)
					.GetAwaiter()
					.GetResult();

				return 0;
			}
#if !NETCOREAPP
			// Print inner exceptions for .NET as they are printed in NETCore 2.1: https://github.com/dotnet/coreclr/pull/15688
			catch (ReflectionTypeLoadException e) when (e.LoaderExceptions?.Length > 0)
			{
				var builder = new StringBuilder();
				foreach (var loaderException in e.LoaderExceptions)
				{
					builder.AppendLine(loaderException.Message);
				}

				logger.LogCritical(e, builder.ToString());
				return -1;
			}
#endif
			catch (Exception e)
			{
				logger.LogCritical(e, e.Message);
				return -1;
			}
			finally
			{
				loggingFactory.Dispose();
			}
		}

		static AsyncCodeConfiguration Configure(ILoggerFactory loggerFactory)
		{
			var xmlConfig = new FileInfo("AsyncGenerator.xml");
			if (xmlConfig.Exists)
			{
				using (var stream = xmlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.LoggerFactory(loggerFactory)
						.ConfigureFromStream<XmlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}
			var ymlConfig = new FileInfo("AsyncGenerator.yml");
			if (ymlConfig.Exists)
			{
				using (var stream = ymlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.LoggerFactory(loggerFactory)
						.ConfigureFromStream<YamlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}

			return null;
		}
	}
}
