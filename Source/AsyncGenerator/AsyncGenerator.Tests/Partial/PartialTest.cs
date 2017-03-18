using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Partial
{
	public class BaseTest
	{
		public IAsyncCodeConfiguration Configure(string folderPath, Action<IProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\AsyncGenerator.sln");
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c =>
					c.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelectionPredicate(o => string.Join("/", o.Folders) == folderPath)
							//.ScanMethodBody(true)
						);
						action?.Invoke(p);
					})

				);
		}

	}


	public class PartialTest : BaseTest
	{
		[Test]
		public async Task TestAnalyzer()
		{
			var generator = new AsyncCodeGenerator();
			var config = Configure("Partial/SimpleClass", p => p
				.ConfigureAnalyzation(a => a
					.MethodConversionFunction(symbol => MethodConversion.Smart)
				)
			);
			try
			{
				await generator.GenerateAsync(config);
			}
			catch (Exception e)
			{
				throw;
			}
		}
	}
}
