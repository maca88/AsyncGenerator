using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.MissingMembers.Input;

namespace AsyncGenerator.Tests.MissingMembers
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public void TestAfterTransformation()
		{
			var config = Configure(o => o
				.ConfigureParsing(p => p
					.AddPreprocessorSymbolName("TEST")
				)
				.ConfigureAnalyzation(a => a
					.ScanForMissingAsyncMembers(true)
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
