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
	public class RemarksFixture : BaseFixture
	{
		[Test]
		public Task TestAddOrReplaceRemarksAfterTransformation()
		{
			return ReadonlyTest(nameof(Remarks), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceMethodRemarks(symbol => "Async remarks!")
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Remarks)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestRemoveRemarksAfterTransformation()
		{
			return ReadonlyTest(nameof(Remarks), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.RemoveMethodRemarks(symbol => true)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("RemarksRemove"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestComplexRemarksAfterTransformation()
		{
			return ReadonlyTest(nameof(Remarks), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceMethodRemarks(symbol => @"
							/// Header
							/// <see cref=""SimpleFile"" />
							/// 
							/// Footer
						")
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("RemarksComplex"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
