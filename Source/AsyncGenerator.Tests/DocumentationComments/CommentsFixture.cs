using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.DocumentationComments.Input;
using NUnit.Framework;


namespace AsyncGenerator.Tests.DocumentationComments
{
	[TestFixture]
	public class CommentsFixture : BaseFixture
	{
		[Test]
		public Task TestPartialTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "PartialType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestPartialTypeReplaceAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplacePartialTypeComments(symbol => symbol.ContainingType != null ? @"
								// Hello
							// This is me
							/// <summary>
							/// Something
							// </summary>
						" : null)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "PartialTypeReplace"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => symbol.Name == nameof(Comments) ? TypeConversion.NewType : TypeConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "NewType"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeReplaceAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => symbol.Name == nameof(Comments) ? TypeConversion.NewType : TypeConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceNewTypeComments(symbol => symbol.ContainingType != null ? @"
								// Hello
							// This is me
							/// <summary>
							/// Something
							// </summary>
						" : null)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "NewTypeReplace"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeRemoveAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => symbol.Name == nameof(Comments) ? TypeConversion.NewType : TypeConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.RemoveNewTypeComments(symbol => true)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "NewTypeRemove"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNewTypeAddMissingAfterTransformation()
		{
			return ReadonlyTest(nameof(Comments), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => symbol.Name == nameof(Comments) ? TypeConversion.NewType : TypeConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.DocumentationComments(d => d
						.AddOrReplaceNewTypeComments(symbol => string.IsNullOrEmpty(symbol.GetDocumentationCommentXml())
							? @"
								// Hello
							// This is me
							/// <summary>
							/// Something
							// </summary>
							"
							: null)
					)
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Comments) + "NewTypeAddMissing"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
