﻿using System;
using System.IO;
using System.Threading;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Internal;
using AsyncGenerator.Logging;
using log4net;
using log4net.Config;

namespace AsyncGenerator.CommandLine
{
	static class Program
	{
		private static readonly ILog Logger;

		static Program()
		{
#if NETCOREAPP2_1
			var configPath = EnvironmentHelper.GetConfigurationFilePath();
			if (!string.IsNullOrEmpty(configPath))
			{
				var logRepository = LogManager.GetRepository(typeof(Program).Assembly);
				Logger = LogManager.GetLogger(logRepository.Name, $"{nameof(AsyncGenerator)}.{nameof(CommandLine)}");
				XmlConfigurator.Configure(logRepository, File.OpenRead(configPath));
			}
#endif
#if NET472 || NET461
			Logger = LogManager.GetLogger($"{nameof(AsyncGenerator)}.{nameof(CommandLine)}");
			XmlConfigurator.Configure();
#endif
		}

		public static int Main(string[] args)
		{
			Logger.Info($"AsyncGenerator{Environment.NewLine}");
			var cancellationSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, e) =>
			{
				Logger.Warn("Canceling operations...");
				cancellationSource.Cancel();
			};
			try
			{
				EnvironmentHelper.Setup();
				var configuration = Configure();
				if (configuration == null)
				{
					Logger.Info("Could not find configuration file:");
					Logger.Info($"\t - please make sure that your working directory contains either AsyncGenerator.xml or AsyncGenerator.yml{Environment.NewLine}");
					Logger.Info("Working directory:");
					Logger.Info($"\t- {Environment.CurrentDirectory}{Environment.NewLine}");
					return 1;
				}

				AsyncCodeGenerator.GenerateAsync(configuration, cancellationSource.Token)
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

		static AsyncCodeConfiguration Configure()
		{
			var xmlConfig = new FileInfo("AsyncGenerator.xml");
			if (xmlConfig.Exists)
			{
				using (var stream = xmlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.LoggerFactory(new Log4NetLoggerFactory())
						.ConfigureFromStream<XmlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}
			var ymlConfig = new FileInfo("AsyncGenerator.yml");
			if (ymlConfig.Exists)
			{
				using (var stream = ymlConfig.OpenRead())
				{
					return AsyncCodeConfiguration.Create()
						.LoggerFactory(new Log4NetLoggerFactory())
						.ConfigureFromStream<YamlFileConfigurator>(stream, Environment.CurrentDirectory);
				}
			}

			return null;
		}
	}
}
