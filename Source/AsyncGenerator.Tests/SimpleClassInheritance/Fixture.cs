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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(4));
				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.That(types[nameof(DerivedClass)].Methods.Count, Is.EqualTo(1));
				Assert.That(types[nameof(DerivedClass)].Methods[0].Conversion, Is.EqualTo(MethodConversion.ToAsync));

				Assert.That(types[nameof(BaseClass)].Methods.Count, Is.EqualTo(1));
				Assert.That(types[nameof(BaseClass)].Methods[0].Conversion, Is.EqualTo(MethodConversion.ToAsync));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseDefaultTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseCustomTokens")));
					})
				)
			);
		}
	}
}
