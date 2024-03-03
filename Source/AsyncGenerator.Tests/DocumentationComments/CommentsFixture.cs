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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "PartialType")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "PartialTypeReplace")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "NewType")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "NewTypeReplace")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "NewTypeRemove")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Comments) + "NewTypeAddMissing")));
					})
				)
			);
		}
	}
}
