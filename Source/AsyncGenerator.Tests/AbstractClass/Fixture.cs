using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.AbstractClass.Input;

namespace AsyncGenerator.Tests.AbstractClass
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAsyncFromInterfaceAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AsyncFromInterface"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestAsyncFromImplementationAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(TestCase) ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("AsyncFromImplementation"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIgnoreFromInterfaceAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInteraface) && symbol.Name == "Read" ? MethodConversion.Ignore : MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("IgnoreFromInterface"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(p => p
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
						Assert.AreEqual(GetOutputFile("TestCaseDefaultTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
