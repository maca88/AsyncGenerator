using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Github.Issue46.Input;
using NUnit.Framework;

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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(0));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCaseWithToken))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseWoMissingMembersWithToken")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCaseWithToken))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					})
				)
			);
		}

		#endregion
	}
}
