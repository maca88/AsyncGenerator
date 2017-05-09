using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
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
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) ? MethodConversion.ToAsync : MethodConversion.Unknown)
					.CancellationTokens(t => t
						.MethodGeneration(symbol =>
						{
							if (symbol.ContainingType.TypeKind == TypeKind.Interface || symbol.OverriddenMethod != null)
							{
								return CancellationTokenMethod.Parameter;
							}
							return CancellationTokenMethod.Parameter | CancellationTokenMethod.SealedNoParameterForward;
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
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) ? MethodConversion.ToAsync : MethodConversion.Unknown)
					.CancellationTokens(t => t
						.Guards(true)
						.MethodGeneration(symbol =>
						{
							if (symbol.ContainingType.TypeKind == TypeKind.Interface || symbol.OverriddenMethod != null)
							{
								return CancellationTokenMethod.Parameter;
							}
							return CancellationTokenMethod.Parameter | CancellationTokenMethod.SealedNoParameterForward;
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
	}
}
