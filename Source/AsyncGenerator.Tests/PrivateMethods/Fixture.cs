using System.Globalization;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.PrivateMethods.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.PrivateMethods
{
	/// <summary>
	/// GitHub: https://github.com/maca88/AsyncGenerator/issues/57
	/// </summary>
	[TestFixture]
	public class Fixture : BaseFixture
	{
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
		public Task TestUnknownAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "ReadInternal" ? MethodConversion.Unknown : MethodConversion.Smart)
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
		public Task TestWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(TestCase) + "WithToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];

						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase) + "NewType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];

						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase) + "NewTypeWithToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task ExplicitInterfaceAfterTransformation()
		{
			return ReadonlyTest(nameof(ExplicitInterface), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(ExplicitInterface)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestUsedByNestedFunctionAfterTransformation()
		{
			return ReadonlyTest(nameof(UsedByNestedFunction), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(UsedByNestedFunction)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[TestCase("cs")]
		[TestCase("de")]
		[TestCase("es")]
		[TestCase("fr")]
		[TestCase("it")]
		[TestCase("ja")]
		[TestCase("ko")]
		[TestCase("pl")]
		[TestCase("pt-BR")]
		[TestCase("tr")]
		[TestCase("zh-Hans")]
		[TestCase("zh-Hant")]
		public async Task TestCS0103LocalizationAfterTransformation(string culture)
		{
			var origUiCluture = CultureInfo.CurrentUICulture;
			try
			{
				var cultureInfo = CultureInfo.GetCultureInfo(culture);
				CultureInfo.CurrentUICulture = cultureInfo;
				CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

				await AsyncCodeGenerator.GenerateAsync(AsyncCodeConfiguration.Create()
					.ConfigureProject(GetTestProjectPath("Localization"), o => o
						.ConfigureAnalyzation(a => a
							.CancellationTokens(true)
							.DocumentSelection(d => d.Name == "PrivateMethods.cs")
							.MethodConversion(symbol => symbol.Name == "List" ? MethodConversion.Smart : MethodConversion.Unknown)
						)
						.ConfigureParsing(p => p
							.AddPreprocessorSymbolName("TEST")
						)
						.ConfigureTransformation(t => t
							.AfterTransformation(result =>
							{
								AssertValidAnnotations(result);
								Assert.AreEqual(1, result.Documents.Count);
								var document = result.Documents[0];
								Assert.NotNull(document.OriginalModified);
								Assert.AreEqual(GetOutputFile("PrivateMethods"), document.Transformed.ToFullString());
							})
						)
						.ApplyChanges(false)));
			}
			finally
			{
				CultureInfo.CurrentUICulture = origUiCluture;
				CultureInfo.DefaultThreadCurrentUICulture = origUiCluture;
			}
		}
	}
}
