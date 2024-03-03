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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(1));

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[test].ChildFunctions.Count, Is.EqualTo(1));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Simple))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Simple) + "WithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Expression))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Expression) + "WithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Nested))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Nested) + "WithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(SimpleAsync))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(NestedCall) + "WithTokens")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TryCatch))));
					})
				)
			);
		}

		[Test]
		public Task TestExtensionMethodAfterTransformation()
		{
			return ReadonlyTest(nameof(ExtensionMethod), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AsyncExtensionMethods(e => e.ProjectFile("AsyncGenerator.Tests", "LinqExtensions.cs"))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ExtensionMethod))));
					})
				)
			);
		}
	}
}
