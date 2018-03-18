using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.TestCases;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.NUnit.Input;

namespace AsyncGenerator.Tests.NUnit
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAssertThatAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThat), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(AssertThat)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<NUnitAsyncCounterpartsFinder>()
			);
		}

		[Test]
		public Task TestAssertThatTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThatTryCatch), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(AssertThatTryCatch)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<NUnitAsyncCounterpartsFinder>()
			);
		}

		[Test]
		public Task TestDoesNotThrowTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(DoesNotThrowTryCatch), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(DoesNotThrowTryCatch)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<NUnitAsyncCounterpartsFinder>()
			);
		}

		[Test]
		public Task TestAssertThatNoTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThatNoToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ? MethodConversion.ToAsync : MethodConversion.Smart)
					.ScanMethodBody(true)
					.CancellationTokens(o => o.RequiresCancellationToken(s => s.Name == "Test" ? false : (bool?)null))
					.PreserveReturnType(o => o.Name == "Test")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(AssertThatNoToken)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<NUnitAsyncCounterpartsFinder>()
			);
		}
	}
}
