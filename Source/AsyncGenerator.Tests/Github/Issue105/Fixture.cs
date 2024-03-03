using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Github.Issue105.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Github.Issue105
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
					.SearchAsyncCounterpartsInInheritedTypes(true)
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
		public Task TestCase2AfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase2), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ?  MethodConversion.Smart: MethodConversion.Unknown)
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.ScanForMissingAsyncMembers(true)
					.CancellationTokens(true)
					.CallForwarding(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];

						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase2))));
					})
				)
			.ConfigureParsing(pp => pp
				.AddPreprocessorSymbolName("TEST")
				.AddPreprocessorSymbolName("ASYNC"))
			);
		}

		[Test]
		public Task TestCase3AfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase3), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ? MethodConversion.Smart : MethodConversion.Unknown)
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.ScanForMissingAsyncMembers(true)
					.SearchForAsyncCounterparts(symbol => symbol.Name != "IsDBNull")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					})
				)
				.ConfigureParsing(pp => pp
					.AddPreprocessorSymbolName("TEST")
					.AddPreprocessorSymbolName("ASYNC"))
			);
		}
	}
}
