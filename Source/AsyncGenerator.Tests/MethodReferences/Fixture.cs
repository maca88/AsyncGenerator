using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.MethodReferences.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.MethodReferences
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test, Repeat(10)]
		public void TestAfterTransformation()
		{
			var config = Configure(nameof(ForwardCall), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.SearchForMethodReferences(symbol => !symbol.GetAttributes().Any(o => o.AttributeClass.Name == nameof(CustomAttribute)))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ForwardCall)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
