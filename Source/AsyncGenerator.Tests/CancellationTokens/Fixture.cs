using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.CancellationTokens.Input;

namespace AsyncGenerator.Tests.CancellationTokens
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestCancellationTokensCustomGenerationAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) ? MethodConversion.ToAsync : MethodConversion.Unknown)
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
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("CustomTokens")));
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensGuardsCustomGenerationAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) ? MethodConversion.ToAsync : MethodConversion.Unknown)
					.CancellationTokens(t => t
						.Guards(true)
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
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("GuardsCustomTokens")));
					})
				)
			);
		}

		[Test]
		public Task TestDefaultParameterAfterTransformation()
		{
			return ReadonlyTest(nameof(DefaultParameter), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo =>
						{
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(DefaultParameter))));
					})
				)
			);
		}

		[Test]
		public Task TestDefaultParameterNoTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(DefaultParameter), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.RequiresCancellationToken(o => o.Name == "Read3")
						.ParameterGeneration(symbolInfo =>
						{
							if (symbolInfo.Symbol.Name == "Read3")
							{
								return MethodCancellationToken.Optional;
							}
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("DefaultParameterNoToken")));
					})
				)
			);
		}

		[Test]
		public Task TestParamsAfterTransformation()
		{
			return ReadonlyTest(nameof(Params), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo =>
						{
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Params))));
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensLocalMethodsGuardedAfterTransformation()
		{
			return ReadonlyTest(nameof(ClassWithLocalAsyncMethods), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.Guards(true)
						.ParameterGeneration(symbolInfo =>
						{
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Console.WriteLine(document.Transformed.ToFullString());
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("ClassWithLocalAsyncMethodsGuarded")));
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensLocalMethodsUnGuardedAfterTransformation()
		{
			return ReadonlyTest(nameof(ClassWithLocalAsyncMethods), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo =>
						{
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Console.WriteLine(document.Transformed.ToFullString());
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("ClassWithLocalAsyncMethodsUnGuarded")));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractClassRequiresCancellationTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractClass), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(AbstractClass) ? MethodConversion.Smart : MethodConversion.Unknown)
					.CancellationTokens(t => t
						.Guards(true)
						.RequiresCancellationToken(o => o.ContainingType.Name == nameof(AbstractClass) ? true : (bool?)null)
						.ParameterGeneration(symbolInfo =>
						{
							return MethodCancellationToken.Required;
						}))
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractClass) + "RequiresToken")));
					})
				)
			);
		}

		[Test, Repeat(5)]
		public Task TestOverrideAfterTransformation()
		{
			return ReadonlyTest(nameof(Override), o => o
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(c => c
						.Guards(true)
						.ParameterGeneration(info => MethodCancellationToken.Required)
						.RequiresCancellationToken(symbol => symbol.Name == "WriteMany" || symbol.Name == "Write"))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Override))));
					})
				)
				.ConfigureParsing(p => p
					.AddPreprocessorSymbolName("TEST")
				));
		}
	}
}
