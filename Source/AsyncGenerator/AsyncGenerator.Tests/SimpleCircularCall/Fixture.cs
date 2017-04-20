using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleCircularCall
{
	[TestFixture]
	public class Fixture : BaseTest<Input.TestCase>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile());
			var method1 = GetMethodName(o => o.Method1);
			var method2 = GetMethodName(o => o.Method2);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].InvokedBy.Count);

				var readFileMethod = methods[readFile];

				var circularMethods = new[]
				{
					method1,
					method2,
					readFile
				};
				IMethodAnalyzationResult method;
				foreach (var circularMethod in circularMethods)
				{
					method = methods[circularMethod];
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
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
