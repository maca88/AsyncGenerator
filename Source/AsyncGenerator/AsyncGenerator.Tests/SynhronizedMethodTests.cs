using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Tests.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class SynhronizedMethodTests : BaseTest<SynhronizedMethod>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var noOptimizationSynhronized = GetMethodName(o => o.NoOptimizationSynhronized);
			var synhronized = GetMethodName(o => o.Synhronized);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.IsTrue(methods[noOptimizationSynhronized].MustRunSynchronized);
				Assert.IsTrue(methods[synhronized].MustRunSynchronized);
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.ToAsync;
					})
					.Callbacks(c => c
						.AfterAnalyzation(afterAnalyzationFn)
					)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
