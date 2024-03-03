using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Tests.Plugins.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Plugins
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestEmptyRegionRemoverAfterTransformation()
		{
			return ReadonlyTest(nameof(EmptyRegions), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(EmptyRegions))));
					})
				)
				.RegisterPlugin<EmptyRegionRemover>()
			);
		}

		[Test]
		public Task TestNestedEmptyRegionRemoverAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedEmptyRegions), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(NestedEmptyRegions))));
					})
				)
				.RegisterPlugin<EmptyRegionRemover>()
			);
		}

		[Test]
		public Task TestTransactionScopeAsyncFlowAfterTransformation()
		{
			return ReadonlyTest(nameof(TransactionScopeAsyncFlow), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TransactionScopeAsyncFlow))));
					})
				)
				.RegisterPlugin<TransactionScopeAsyncFlowAdder>()
			);
		}
	}
}
