using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.AsyncProperites.Input;

namespace AsyncGenerator.Tests.AsyncProperites
{
	[TestFixture]
	public class GetterFixture : BaseFixture
	{
		[Test]
		public Task TestGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(Getter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Getter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGetterWithAsyncPartAfterTransformation()
		{
			return ReadonlyTest(nameof(GetterWithAsyncPart), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(GetterWithAsyncPart)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGetterNoConversionAfterTransformation()
		{
			return ReadonlyTest(nameof(Getter), p => p
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
						Assert.AreEqual(GetOutputFile("GetterNoConversion"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestArrowGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(ArrowGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ArrowGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAbstractGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AbstractGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAbstractGetterNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractGetter), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AbstractGetterNewType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestInterfaceGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(InterfaceGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(InterfaceGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestInterfaceGetterNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(InterfaceGetter), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("InterfaceGetterNewType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestExternalGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(ExternalGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ExternalGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAbstractInterfaceGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractInterfaceGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ?  MethodConversion.Smart : MethodConversion.Unknown)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AbstractInterfaceGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAbstractInterfaceGetterWithTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractInterfaceGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ? MethodConversion.Smart : MethodConversion.Unknown)
					.PropertyConversion(true)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AbstractInterfaceGetterWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestListGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(ListGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ListGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestListGetterWithTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(ListGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("ListGetterWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestArgumentGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(ArgumentGetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ArgumentGetter)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
