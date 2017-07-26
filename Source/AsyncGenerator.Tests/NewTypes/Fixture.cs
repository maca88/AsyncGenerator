using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.NewTypes.Input;

namespace AsyncGenerator.Tests.NewTypes
{
	[TestFixture]
	public class Fixture : BaseFixture<NestedClasses>
	{
		[Test]
		public void TestNestedClassesAfterTransformation()
		{
			var config = Configure(nameof(NestedClasses), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedClasses) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol =>
					{
						return symbol.GetAttributes().Any(o => o.AttributeClass.Name == "CustomAttribute")
							? MethodConversion.Smart
							: MethodConversion.Unknown;
					})
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NestedClasses)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNestedClassesWithTokensAfterTransformation()
		{
			var config = Configure(nameof(NestedClasses), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedClasses) ? TypeConversion.NewType : TypeConversion.Unknown)
					.CancellationTokens(true)
					.MethodConversion(symbol =>
					{
						return symbol.GetAttributes().Any(o => o.AttributeClass.Name == "CustomAttribute")
							? MethodConversion.Smart
							: MethodConversion.Unknown;
					})
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("NestedClassesWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNonAsyncNewTypeAfterTransformation()
		{
			var config = Configure(nameof(NonAsync), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NonAsync) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestInheritanceNewTypeAfterTransformation()
		{
			var config = Configure(nameof(Inheritance), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Inheritance)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestInheritanceIgnoreBaseAfterTransformation()
		{
			var config = Configure(nameof(Inheritance), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol =>
					{
						return symbol.Name == nameof(Inheritance) ? TypeConversion.Ignore : TypeConversion.NewType;
					})
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("InheritanceIgnoredBase"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestInheritanceIgnoreBaseWithTokensAfterTransformation()
		{
			var config = Configure(nameof(Inheritance), p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.TypeConversion(symbol =>
					{
						return symbol.Name == nameof(Inheritance) ? TypeConversion.Ignore : TypeConversion.NewType;
					})
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("InheritanceIgnoredBaseWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestMissingMembersAfterTransformation()
		{
			var config = Configure(nameof(MissingMembers), o => o
				.ConfigureParsing(p => p
					.AddPreprocessorSymbolName("TEST")
				)
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
					.TypeConversion(symbol =>
					{
						return symbol.Name == nameof(MissingMembers) ? TypeConversion.NewType : TypeConversion.Unknown;
					})
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("MissingMembersOriginal"), document.OriginalModified.ToFullString());
						Assert.AreEqual(GetOutputFile(nameof(MissingMembers)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestNestedCopyAfterTransformation()
		{
			var config = Configure(nameof(NestedCopy), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(AbstractNestedCopy) ? TypeConversion.Ignore : TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NestedCopy)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
