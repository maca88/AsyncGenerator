using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.MissingMembers.Input;

namespace AsyncGenerator.Tests.MissingMembers
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public void TestAfterTransformation()
		{
			var config = Configure(nameof(TestCase), o => o
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

		[Test]
		public void TestGetterPropertyAfterTransformation()
		{
			var config = Configure(nameof(GetterProperty), o => o
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
						Assert.AreEqual(GetOutputFile(nameof(GetterProperty)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNestedTypeAfterTransformation()
		{
			var config = Configure(nameof(NestedType), o => o
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
						Assert.AreEqual(GetOutputFile(nameof(NestedType)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNestedTypeNewTypeAfterTransformation()
		{
			var config = Configure(nameof(NestedType), o => o
				.ConfigureParsing(p => p
					.AddPreprocessorSymbolName("TEST")
				)
				.ConfigureAnalyzation(a => a
					.ScanForMissingAsyncMembers(true)
					.TypeConversion(symbol => symbol.Name == nameof(NestedType) ? TypeConversion.NewType : TypeConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("NestedTypeNewType"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
