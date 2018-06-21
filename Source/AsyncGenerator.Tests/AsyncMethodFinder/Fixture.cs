using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestCustomLinqExtensionsAfterTransformation()
		{
			return ReadonlyTest(nameof(CustomLinqExtensions), p => p
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
		}

		[Test]
		public Task TestIgnoreSomeAsyncMethodsAfterTransformation()
		{
			return ReadonlyTest(nameof(IgnoreSomeAsyncMethods), p => p
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
		}

		[Test]
		public Task TestGenericParameterAfterTransformation()
		{
			return ReadonlyTest(nameof(GenericParameter), p => p
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
		}

		[Test]
		public Task TestGuardsAfterTransformation()
		{
			return ReadonlyTest(nameof(Guards), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(o => o
						.RequiresCancellationToken(symbol => true)
						.Guards(true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Guards)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGenericTypeParameterAfterTransformation()
		{
			return ReadonlyTest(nameof(GenericTypeParameter), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(GenericTypeParameter)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
