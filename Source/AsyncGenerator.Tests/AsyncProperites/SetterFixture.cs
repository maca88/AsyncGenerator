using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.AsyncProperites.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.AsyncProperites
{
	[TestFixture]
	public class SetterFixture : BaseFixture
	{
		[Test]
		public Task TestSetterAfterTransformation()
		{
			return ReadonlyTest(nameof(Setter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Setter))));
					})
				)
			);
		}

		[Test]
		public Task TestArrowSetterAfterTransformation()
		{
			return ReadonlyTest(nameof(ArrowSetter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ArrowSetter))));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractSetterAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractSetter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractSetter))));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractSetterNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractSetter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("AbstractSetterNewType")));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractInterfaceSetterAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractInterfaceSetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ? MethodConversion.Smart : MethodConversion.Unknown)
					.PropertyConversion(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractInterfaceSetter))));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractInterfaceSetterWithTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractInterfaceSetter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("AbstractInterfaceSetterWithTokens")));
					})
				)
			);
		}

		[Test]
		public Task TestSetAccessorAfterTransformation()
		{
			return ReadonlyTest(nameof(SetAccessor), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(SetAccessor))));
					})
				)
			);
		}

		[Test]
		public Task TestInitSetterAfterTransformation()
		{
			return ReadonlyTest(nameof(InitSetter), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(InitSetter))));
					})
				)
			);
		}

		[Test]
		public Task TestInitSetterNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(InitSetter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PropertyConversion(true)
					.TypeConversion(o => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile($"{nameof(InitSetter)}NewType")));
					})
				)
			);
		}
	}
}
