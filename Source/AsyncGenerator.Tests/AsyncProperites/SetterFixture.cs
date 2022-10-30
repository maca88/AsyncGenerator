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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Setter)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ArrowSetter)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AbstractSetter)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AbstractSetterNewType"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AbstractInterfaceSetter)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AbstractInterfaceSetterWithTokens"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(SetAccessor)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(InitSetter)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile($"{nameof(InitSetter)}NewType"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
