using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Tests.SimpleClassInheritance.Input;
using AsyncGenerator.Tests.TestCases;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleClassInheritance
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var read = GetMethodName(o => o.Read);
			var readFile = GetMethodName<DerivedClass>(o => o.ReadFile);

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(4, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, types[nameof(DerivedClass)].Methods.Count);
				Assert.AreEqual(MethodConversion.ToAsync, types[nameof(DerivedClass)].Methods[0].Conversion);

				Assert.AreEqual(1, types[nameof(BaseClass)].Methods.Count);
				Assert.AreEqual(MethodConversion.ToAsync, types[nameof(BaseClass)].Methods[0].Conversion);
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseDefaultTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensCustomGenerationAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo =>
						{
							if (symbolInfo.Symbol.ContainingType.TypeKind == TypeKind.Interface || symbolInfo.Symbol.OverriddenMethod != null)
							{
								return MethodCancellationToken.Required;
							}
							return MethodCancellationToken.Required | MethodCancellationToken.SealedForwardNone;
						}))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseCustomTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
