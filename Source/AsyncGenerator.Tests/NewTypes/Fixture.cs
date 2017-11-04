using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestNestedClassesAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedClasses), p => p
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
		}

		[Test]
		public Task TestNestedClassesWithTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedClasses), p => p
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
		}

		[Test]
		public Task TestNestedClassesWithTokensForNestedClassAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedClasses), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedClasses) ? TypeConversion.NewType : TypeConversion.Unknown)
					.CancellationTokens(t => t
						.RequiresCancellationToken(o => o.ContainingType.Name != nameof(NestedClasses)))
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
						Assert.AreEqual(GetOutputFile("NestedClassesWithTokensForNestedClass"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNonAsyncNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(NonAsync), p => p
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
		}

		[Test]
		public Task TestInheritanceNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(Inheritance), p => p
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
		}

		[Test]
		public Task TestInheritanceIgnoreBaseAfterTransformation()
		{
			return ReadonlyTest(nameof(Inheritance), p => p
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
		}

		[Test]
		public Task TestInheritanceIgnoreBaseWithTokensAfterTransformation()
		{
			return ReadonlyTest(nameof(Inheritance), p => p
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
		}

		[Test]
		public Task TestMissingMembersAfterTransformation()
		{
			return ReadonlyTest(nameof(MissingMembers), o => o
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
		}

		[Test]
		public Task TestNestedCopyAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedCopy), p => p
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
		}

		[Test, Repeat(10)]
		public Task TestNestedCopyIgnoreReadMethodAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedCopy), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(AbstractNestedCopy) ? TypeConversion.Ignore : TypeConversion.NewType)
					.MethodConversion(symbol => symbol.Name == "Read" ? MethodConversion.Ignore : MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("NestedCopyIgnoreRead"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedCopyCopyReadMethodAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedCopy), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(AbstractNestedCopy) ? TypeConversion.Ignore : TypeConversion.NewType)
					.MethodConversion(symbol => symbol.Name == "Read" ? MethodConversion.Copy : MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("NestedCopyIgnoreRead"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestDerivedClassesAfterTransformation()
		{
			return ReadonlyTest(nameof(DerivedClasses), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(DerivedClasses)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestDerivedClassesBaseNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(DerivedClasses), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(BaseClass) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.Null(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(DerivedClasses)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestPartialDerivedClassesAfterTransformation()
		{
			return ReadonlyTest(nameof(DerivedClasses), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("PartialDerivedClasses"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestDerivedAsyncClassAfterTransformation()
		{
			return ReadonlyTest(nameof(DerivedAsyncClass), p => p
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
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(DerivedAsyncClass)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestSubNestedClassAfterTransformation()
		{
			return ReadonlyTest(nameof(SubNestedClass), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(SubNestedClass) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(SubNestedClass)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedDerivedAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedDerivedAsync), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedDerivedAsync) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => symbol.Name == "Write" ? MethodConversion.Smart : MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NestedDerivedAsync)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNestedDerivedAsyncIgnoreAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedDerivedAsync), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedDerivedAsync) ? TypeConversion.Ignore : TypeConversion.Unknown)
					.MethodConversion(symbol => symbol.Name == "Write" ? MethodConversion.Smart : MethodConversion.Unknown)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		[Test]
		public Task TestNestedDerivedAsyncWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedDerivedAsyncWithToken), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => symbol.Name == nameof(NestedDerivedAsyncWithToken) ? TypeConversion.NewType : TypeConversion.Unknown)
					.MethodConversion(symbol => symbol.Name == "Write" ? MethodConversion.Smart : MethodConversion.Unknown)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NestedDerivedAsyncWithToken)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMethodWithDelegateAfterTransformation()
		{
			return ReadonlyTest(nameof(MethodWithDelegate), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];

						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MethodWithDelegate)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
