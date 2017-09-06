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
		public void TestAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestUnknownConversionAfterTransformation()
		{
			var config = Configure(nameof(TestCase), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestIgnoreConversionAfterTransformation()
		{
			var read = GetMethodName<TestCase>(o => o.Read());
			var config = Configure(nameof(TestCase), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		#endregion

		#region WithToken

		[Test]
		public void TestWithTokenAfterTransformation()
		{
			var config = Configure(nameof(TestCaseWithToken), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestWithoutMissingMembersWithTokenAfterTransformation()
		{
			var config = Configure(nameof(TestCaseWithToken), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestUnknownConversionWithTokenAfterTransformation()
		{
			var config = Configure(nameof(TestCaseWithToken), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestIgnoreConversionWithTokenAfterTransformation()
		{
			var read = GetMethodName<TestCaseWithToken>(o => o.Read());
			var config = Configure(nameof(TestCaseWithToken), p => p
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		#endregion
	}
}
