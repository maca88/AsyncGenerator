using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using NUnit.Framework;

namespace AsyncGenerator.Tests.CustomReturnType
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.TestCase>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var getData = GetMethodName(o => o.GetData());
			var getDataAsync = GetMethodName(o => o.GetDataAsync());

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(MethodConversion.Ignore, methods[getData].Conversion);
				Assert.AreEqual(MethodConversion.Ignore, methods[getDataAsync].Conversion);
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == getData ?  MethodConversion.ToAsync : MethodConversion.Unknown;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestCancellationTokensAfterAnalyzation()
		{
			var getData = GetMethodName(o => o.GetData());
			var getDataAsync = GetMethodName(o => o.GetDataAsync());

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(MethodConversion.Ignore, methods[getData].Conversion);
				Assert.AreEqual(MethodConversion.Ignore, methods[getDataAsync].Conversion);
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(t => t
						.MethodGeneration(symbol => MethodCancellationToken.Parameter | MethodCancellationToken.NoParameterForward))
					.MethodConversion(symbol =>
					{
						return symbol.Name == getData ? MethodConversion.ToAsync : MethodConversion.Unknown;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
