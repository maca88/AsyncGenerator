using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.TryCatch.Input;

namespace AsyncGenerator.Tests.TryCatch
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestElementAccessAfterTransformation()
		{
			return ReadonlyTest(nameof(ElementAccess), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ElementAccess)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCastAfterTransformation()
		{
			return ReadonlyTest(nameof(Cast), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Cast)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIfStatementAfterTransformation()
		{
			return ReadonlyTest(nameof(IfStatement), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(IfStatement)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestObjectCreationAfterTransformation()
		{
			return ReadonlyTest(nameof(ObjectCreation), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObjectCreation)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestInvocationAfterTransformation()
		{
			return ReadonlyTest(nameof(Invocation), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Invocation)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAutoPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(AutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AutoProperty)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCustomPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(CustomProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(CustomProperty)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
