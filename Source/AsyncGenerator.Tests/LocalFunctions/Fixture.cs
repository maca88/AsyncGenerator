using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Tests.LocalFunctions.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.LocalFunctions
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestSimpleAfterAnalyzation()
		{
			var test = GetMethodName<Simple>(o => o.Test());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[test].ChildFunctions.Count);
				//Assert.AreEqual(1, methods[expression].ChildFunctions.Count);
			}

			return ReadonlyTest(nameof(Simple), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestSimpleAfterTransformation()
		{
			return ReadonlyTest(nameof(Simple), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Simple)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestSimpleCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(Simple), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Simple) + "WithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestExpressionAfterTransformation()
		{
			return ReadonlyTest(nameof(Expression), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Expression)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestExpressionCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(Expression), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Expression) + "WithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedAfterTransformation()
		{
			return ReadonlyTest(nameof(Nested), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Nested)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(Nested), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Nested) + "WithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestSimpleAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(SimpleAsync), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(SimpleAsync)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedCallCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedCall), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(NestedCall) + "WithTokens"), document.Transformed.ToFullString());
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
			);
		}
	}
}
