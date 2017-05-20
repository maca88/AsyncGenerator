using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.NewTypes.Input;

namespace AsyncGenerator.Tests.NewTypes
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public void TestNewTypeAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(TestCase) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNonAsyncNewTypeAfterTransformation()
		{
			var config = Configure(nameof(NonAsync), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NonAsync) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestInheritanceNewTypeAfterTransformation()
		{
			var config = Configure(nameof(Inheritance), p => p
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
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Inheritance)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
