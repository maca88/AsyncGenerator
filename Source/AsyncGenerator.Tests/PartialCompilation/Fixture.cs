using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.PartialCompilation.Input;

namespace AsyncGenerator.Tests.PartialCompilation
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestCtorAfterTransformation()
		{
			return ReadonlyTest(nameof(Ctor), o => o
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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
						Assert.AreEqual(GetOutputFile(nameof(Ctor)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCtorMultiOverloadsAfterTransformation()
		{
			return ReadonlyTest(nameof(CtorMultiOverloads), o => o
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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
						Assert.AreEqual(GetOutputFile(nameof(CtorMultiOverloads)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGenericCtorMultiOverloadsAfterTransformation()
		{
			return ReadonlyTest(nameof(GenericCtorMultiOverloads), o => o
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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
						Assert.AreEqual(GetOutputFile(nameof(GenericCtorMultiOverloads)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGenericCtorMultiOverloadsDiffReturnTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(GenericCtorMultiOverloadsDiffReturnType), o => o
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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
						Assert.AreEqual(GetOutputFile(nameof(GenericCtorMultiOverloadsDiffReturnType)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
