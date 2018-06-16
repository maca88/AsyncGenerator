using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Github.Issue46.Input;
using AsyncGenerator.Tests.NewTypes.Input;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace AsyncGenerator.Tests.Github.Issue46
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		#region Default

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestUnknownConversionAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIgnoreConversionAfterTransformation()
		{
			var read = GetMethodName<TestCase>(o => o.Read());
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == read ? MethodConversion.Ignore : MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		#endregion

		#region WithToken

		[Test]
		public Task TestWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCaseWithToken), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => MethodConversion.Smart)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCaseWithToken)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestWithoutMissingMembersWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCaseWithToken), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseWoMissingMembersWithToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestUnknownConversionWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCaseWithToken), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCaseWithToken)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestIgnoreConversionWithTokenAfterTransformation()
		{
			var read = GetMethodName<TestCaseWithToken>(o => o.Read());
			return ReadonlyTest(nameof(TestCaseWithToken), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => symbol.Name == read ? MethodConversion.Ignore : MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		#endregion
	}
}
