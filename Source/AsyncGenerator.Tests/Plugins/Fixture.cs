using System;
using System.Linq;
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
		public void TestEmptyRegionRemoverAfterTransformation()
		{
			var config = Configure(nameof(EmptyRegions), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(EmptyRegions)), document.Transformed.ToFullString());
					})
				)
				.RegisterPlugin<EmptyRegionRemover>()
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
