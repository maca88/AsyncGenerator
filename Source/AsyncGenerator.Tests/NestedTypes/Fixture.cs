using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.NestedTypes.Input;

namespace AsyncGenerator.Tests.NestedTypes
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
						Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
						Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].NestedTypes.Count);
					})
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
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
						Assert.AreEqual(GetOutputFile("TestCaseOriginal"), document.OriginalModified.ToFullString());
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test, Repeat(5)]
		public Task TestMultipleNestedAfterTransformation()
		{
			return ReadonlyTest(nameof(MultipleNested), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == "Nested1" ? TypeConversion.Ignore : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MultipleNested)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
