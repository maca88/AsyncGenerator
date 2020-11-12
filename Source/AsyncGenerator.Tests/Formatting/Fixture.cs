using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Cref.Input;
using AsyncGenerator.Tests.Formatting.Input;

namespace AsyncGenerator.Tests.Formatting
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		[Repeat(5)]
		public Task TestAwaitAfterTransformation()
		{
			return ReadonlyTest(nameof(Await), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Await)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(Async), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.PreserveReturnType(symbol => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Async)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestConfigureAwaitAfterTransformation()
		{
			return ReadonlyTest(nameof(ConfigureAwait), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => true)
				)
				.ConfigureTransformation(t => t
					.ConfigureAwaitArgument(false)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ConfigureAwait)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIndentationAfterTransformation()
		{
			return ReadonlyTest(nameof(Indentation), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => true)
					.CancellationTokens(t => t
						.Guards(true)
					)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Indentation)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMethodBodyBracesAfterTransformation()
		{
			return ReadonlyTest(nameof(MethodBodyBraces), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.CallForwarding(true)
					.CancellationTokens(t => t
						.Guards(true)
						.RequiresCancellationToken(s => true)
					)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MethodBodyBraces)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMultilineParametersAfterTransformation()
		{
			return ReadonlyTest(nameof(MultilineParameters), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.Guards(true)
					)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MultilineParameters)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedClassesAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedClasses), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(NestedClasses)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIndentSwitchAfterTransformation()
		{
			return ReadonlyTest(nameof(IndentSwitch), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(IndentSwitch)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIndentConditionAfterTransformation()
		{
			return ReadonlyTest(nameof(IndentCondition), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(IndentCondition)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMixingIndentAfterTransformation()
		{
			return ReadonlyTest(nameof(MixingIndent), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MixingIndent)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
