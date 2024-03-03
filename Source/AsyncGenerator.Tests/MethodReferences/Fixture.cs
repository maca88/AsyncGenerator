using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(ForwardCall), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.SearchForMethodReferences(symbol => !symbol.GetAttributes().Any(o => o.AttributeClass.Name == nameof(CustomAttribute)))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ForwardCall))));
					})
				)
			);
		}
	}
}
