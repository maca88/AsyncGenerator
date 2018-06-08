using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), o => o
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
		}

		[Test]
		public Task TestGetterPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(GetterProperty), o => o
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
		}

		[Test]
		public Task TestNestedTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedType), o => o
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
		}

		[Test]
		public Task TestNestedTypeNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedType), o => o
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
		}

		[Test]
		public Task TestObsoleteAbstractAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteAbstract), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
					.AddPreprocessorSymbolName("OVERRIDE")
				)
			);
		}

		[Test]
		public Task TestObsoleteAbstractWithoutOverridesAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteAbstract), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObsoleteAbstract)), document.Transformed.ToFullString());
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
				)
			);
		}

		[Test]
		public Task TestObsoleteAbstractTokenAbstractAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteAbstractToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
					.AddPreprocessorSymbolName("OVERRIDE")
				)
			);
		}

		[Test]
		public Task TestObsoleteAbstractTokenWithoutOverridesAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteAbstractToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObsoleteAbstract) + "Token"), document.Transformed.ToFullString());
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
				)
			);
		}

		[Test]
		public Task TestObsoleteInterfaceAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteInterface), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
					.AddPreprocessorSymbolName("OVERRIDE")
				)
			);
		}

		[Test]
		public Task TestObsoleteInterfaceWithoutOverridesAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteInterface), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObsoleteInterface)), document.Transformed.ToFullString());
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
				)
			);
		}

		[Test]
		public Task TestObsoleteInterfaceTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteInterfaceToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
					.AddPreprocessorSymbolName("OVERRIDE")
				)
			);
		}

		[Test]
		public Task TestObsoleteInterfaceTokenWithoutOverridesAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteInterfaceToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObsoleteInterface) + "Token"), document.Transformed.ToFullString());
					})
				)
				.ConfigureParsing(pr => pr
					.AddPreprocessorSymbolName("OBSOLETE")
				)
			);
		}
	}
}
