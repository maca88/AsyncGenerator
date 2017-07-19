using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.MultiAsyncCounterparts.Input;

namespace AsyncGenerator.Tests.MultiAsyncCounterparts
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public void TestAfterTransformation()
		{
			var config = Configure(nameof(TaskRunFunc), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => true)
					.CancellationTokens(t => t
					.RequiresCancellationToken(symbol => true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TaskRunFunc)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
