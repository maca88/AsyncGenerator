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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Method) + "NewType"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Method) + "Partial"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(SelfReference) + "NewType"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ArgumentFunction) + "NewType"), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Variable)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Variable)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
