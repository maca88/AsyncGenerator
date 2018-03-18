using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.TestCases;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.AnonymousFunctions.Input;

namespace AsyncGenerator.Tests.AnonymousFunctions
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestPreserveReturnTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
					.PreserveReturnType(symbol => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("PreserveReturnType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestPreserveReturnTypeWithoutTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.RequiresCancellationToken(s => s.ContainingType.Name == "TestCase" ? (bool?)false : null))
					.PreserveReturnType(symbol => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("PreserveReturnTypeWithoutTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMethodWithDelegateAfterTransformation()
		{
			return ReadonlyTest(nameof(MethodWithDelegate), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(MethodWithDelegate)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestArrayOfDelegatesAfterTransformation()
		{
			return ReadonlyTest(nameof(ArrayOfDelegates), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => nameof(ArrayOfDelegates) == symbol.Name
						? TypeConversion.NewType
						: TypeConversion.Unknown)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ArrayOfDelegates)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestVariableAfterTransformation()
		{
			return ReadonlyTest(nameof(Variable), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Variable)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(TryCatch), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(TryCatch)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<RunnerAsyncCountepartFinder>()
			);
		}

		[Test]
		public Task TestVoidTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(VoidTryCatch), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(VoidTryCatch)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<RunnerAsyncCountepartFinder>()
			);
		}

		[Test]
		public Task TestReturnTypeMismatchAfterTransformation()
		{
			return ReadonlyTest(nameof(ReturnTypeMismatch), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(ReturnTypeMismatch)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
