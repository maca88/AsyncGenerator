using AsyncGenerator.Core;
using AsyncGenerator.Tests.Nameof.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Nameof
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public void TestNewTypeAfterTransformation()
		{
			var config = Configure(nameof(Method), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Method) + "NewType"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestPartialAfterTransformation()
		{
			var config = Configure(nameof(Method), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Method) + "Partial"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
