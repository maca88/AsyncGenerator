using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.CSharpFeatures.Input;

namespace AsyncGenerator.Tests.CSharpFeatures
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestMethodConditionalAccessExpressionAfterTransformation()
		{
			return ReadonlyTest(nameof(MethodConditionalAccessExpression), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MethodConditionalAccessExpression)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestConditionalAccessWithNullCoalescingAfterTransformation()
		{
			return ReadonlyTest(nameof(ConditionalAccessWithNullCoalescing), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ConditionalAccessWithNullCoalescing)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCustomEnumerableAfterTransformation()
		{
			return ReadonlyTest(nameof(CustomEnumerable), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(CustomEnumerable)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
