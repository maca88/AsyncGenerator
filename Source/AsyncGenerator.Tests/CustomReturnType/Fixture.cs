using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Tests.CustomReturnType.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.CustomReturnType
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var getData = GetMethodName(o => o.GetData());
			var getDataAsync = GetMethodName(o => o.GetDataAsync());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(2, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(MethodConversion.Ignore, methods[getData].Conversion);
				Assert.AreEqual(MethodConversion.Ignore, methods[getDataAsync].Conversion);
			}

			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == getData ?  MethodConversion.ToAsync : MethodConversion.Unknown;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensAfterAnalyzation()
		{
			var getData = GetMethodName(o => o.GetData());
			var getDataAsync = GetMethodName(o => o.GetDataAsync());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(2, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(MethodConversion.Ignore, methods[getData].Conversion);
				Assert.AreEqual(MethodConversion.Ignore, methods[getDataAsync].Conversion);
			}

			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(t => t
						.ParameterGeneration(symbol => MethodCancellationToken.Required | MethodCancellationToken.ForwardNone))
					.MethodConversion(symbol =>
					{
						return symbol.Name == getData ? MethodConversion.ToAsync : MethodConversion.Unknown;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestSameTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(SameType), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}
	}
}
