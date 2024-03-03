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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Getter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(GetterWithAsyncPart))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("GetterNoConversion")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ArrowGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("AbstractGetterNewType")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(InterfaceGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("InterfaceGetterNewType")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ExternalGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractInterfaceGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("AbstractInterfaceGetterWithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ListGetter))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("ListGetterWithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ArgumentGetter))));
					})
				)
			);
		}

		[Test]
		public Task TestGetAccessorAfterTransformation()
		{
			return ReadonlyTest(nameof(GetAccessor), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureParsing(pp => pp
					.AddPreprocessorSymbolName("TEST"))
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(GetAccessor))));
					})
				)
			);
		}
	}
}
