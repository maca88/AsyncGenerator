using System;
using System.Linq;
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
		public void TestCancellationTokensCustomGenerationAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("CustomTokens"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestCancellationTokensGuardsCustomGenerationAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("GuardsCustomTokens"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestDefaultParameterAfterTransformation()
		{
			var config = Configure(nameof(DefaultParameter), p => p
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(DefaultParameter)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestDefaultParameterNoTokenAfterTransformation()
		{
			var config = Configure(nameof(DefaultParameter), p => p
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("DefaultParameterNoToken"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestParamsAfterTransformation()
		{
			var config = Configure(nameof(Params), p => p
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Params)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
