using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.DocumentationComments.Input;

namespace AsyncGenerator.Tests.DocumentationComments
{
	[TestFixture]
	public class SummaryFixture : BaseFixture
	{
		[Test]
		public Task TestAddOrReplaceSummaryAfterTransformation()
		{
			return ReadonlyTest(nameof(Summary), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceMethodSummary(symbol => "Async summary!")
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Summary))));
					})
				)
			);
		}

		[Test]
		public Task TestRemoveSummaryAfterTransformation()
		{
			return ReadonlyTest(nameof(Summary), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.RemoveMethodSummary(symbol => true)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("SummaryRemove")));
					})
				)
			);
		}

		[Test]
		public Task TestComplexSummaryAfterTransformation()
		{
			return ReadonlyTest(nameof(Summary), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceMethodSummary(symbol => @"
							/// Header
							/// <see cref=""SimpleFile"" />
							/// 
							/// Footer
						")
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("SummaryComplex")));
					})
				)
			);
		}
	}
}
