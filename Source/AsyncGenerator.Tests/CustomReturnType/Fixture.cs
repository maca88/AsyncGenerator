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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(2));

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[getData].Conversion, Is.EqualTo(MethodConversion.Ignore));
				Assert.That(methods[getDataAsync].Conversion, Is.EqualTo(MethodConversion.Ignore));
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
						Assert.That(result.Documents.Count, Is.EqualTo(0));
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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(2));

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[getData].Conversion, Is.EqualTo(MethodConversion.Ignore));
				Assert.That(methods[getDataAsync].Conversion, Is.EqualTo(MethodConversion.Ignore));
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
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					})
				)
			);
		}
	}
}
