using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Partial.SimpleClass
{
	public class PartialTest
	{
		[Test]
		public async Task TestAnalyzer()
		{
			var slnFilePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\AsyncGenerator.sln");
			var generator = new AsyncCodeGenerator();
			var config = AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c =>
					c.ConfigureProject("AsyncGenerator.Tests", p =>
						p.ConfigureAnalyzation(a => a
							.DocumentSelectionPredicate(o => o.Name == "TestCase" && string.Join("/", o.Folders) == "Partial/SimpleClass" )
							//.MethodConversionFunction(symbol => MethodConversion.Smart)
							//.ScanMethodBody(true)
						)
							
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
