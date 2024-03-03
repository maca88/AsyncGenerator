using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Nameof.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Nameof
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(Method), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Method) + "NewType")));
					})
				)
			);
		}

		[Test]
		public Task TestPartialAfterTransformation()
		{
			return ReadonlyTest(nameof(Method), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Method) + "Partial")));
					})
				)
			);
		}

		[Test]
		public Task TestSelfReferenceNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(SelfReference), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(SelfReference) + "NewType")));
					})
				)
			);
		}

		[Test]
		public Task TestArgumentFunctionNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(ArgumentFunction), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.TypeConversion(symbol => TypeConversion.NewType)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ArgumentFunction) + "NewType")));
					})
				)
			);
		}

		[Test]
		public Task TestVariableAfterTransformation()
		{
			return ReadonlyTest(nameof(Variable), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Variable))));
					})
				)
			);
		}

		[Test]
		public Task TestVariableSearchForMethodReferencesAfterTransformation()
		{
			return ReadonlyTest(nameof(Variable), p => p
				.ConfigureAnalyzation(a => a
						.MethodConversion(symbol => MethodConversion.ToAsync)
					.SearchForMethodReferences(symbol => symbol.Name != nameof(Test))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Variable))));
					})
				)
			);
		}
	}
}
