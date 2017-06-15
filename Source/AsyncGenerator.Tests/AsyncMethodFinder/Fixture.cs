using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.AsyncMethodFinder.Input;

namespace AsyncGenerator.Tests.AsyncMethodFinder
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public void TestCustomLinqExtensionsAfterTransformation()
		{
			var config = Configure(nameof(CustomLinqExtensions), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AsyncExtensionMethods(e => e.ProjectFile("AsyncGenerator.Tests", "LinqExtensions.cs"))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(CustomLinqExtensions)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNUnitAssertThatAfterTransformation()
		{
			var config = Configure(nameof(NUnitAssertThat), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NUnitAssertThat)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<NUnitAsyncCounterpartFinder>()
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(() => generator.GenerateAsync(config));
		}

		[Test]
		public void TestIgnoreSomeAsyncMethodsAfterTransformation()
		{
			var config = Configure(nameof(IgnoreSomeAsyncMethods), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.SearchForAsyncCounterparts(symbol => symbol.Name == "Write" ? false : true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(IgnoreSomeAsyncMethods)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(() => generator.GenerateAsync(config));
		}

		[Test]
		public void TestGenericParameterAfterTransformation()
		{
			var config = Configure(nameof(GenericParameter), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(GenericParameter)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(() => generator.GenerateAsync(config));
		}
	}
}
