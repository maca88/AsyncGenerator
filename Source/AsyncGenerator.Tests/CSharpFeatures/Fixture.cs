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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(MethodConditionalAccessExpression))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ConditionalAccessWithNullCoalescing))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(CustomEnumerable))));
					})
				)
			);
		}

		[Test]
		public Task TestInModifierAfterTransformation()
		{
			return ReadonlyTest(nameof(InModifier), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(InModifier))));
					})
				)
			);
		}
	}
}
