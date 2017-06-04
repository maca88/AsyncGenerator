using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Cref.Input;
using AsyncGenerator.Tests.Formatting.Input;

namespace AsyncGenerator.Tests.Formatting
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		[Repeat(5)]
		public void TestAwaitAfterTransformation()
		{
			var config = Configure(nameof(Await), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Await)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(() => generator.GenerateAsync(config));
		}

		[Test]
		public void TestAsyncAfterTransformation()
		{
			var config = Configure(nameof(Async), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Async)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(() => generator.GenerateAsync(config));
		}
	}
}
