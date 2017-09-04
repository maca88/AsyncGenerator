using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.NestedTypes.Input;

namespace AsyncGenerator.Tests.NestedTypes
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var config = Configure(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
						Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
						Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types[0].NestedTypes.Count);
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
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
						Assert.AreEqual(GetOutputFile("TestCaseOriginal"), document.OriginalModified.ToFullString());
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test, Repeat(5)]
		public void TestMultipleNestedAfterTransformation()
		{
			var config = Configure(nameof(MultipleNested), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == "Nested1" ? TypeConversion.Ignore : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MultipleNested)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
