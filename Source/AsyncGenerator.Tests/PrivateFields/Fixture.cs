using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.PrivateFields.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.PrivateFields
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test, Repeat(5)]
		public Task TestUnusedFieldAfterTransformation()
		{
			return ReadonlyTest(nameof(UnusedField), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(UnusedField))));
					})
				)
			);
		}

		[Test]
		public Task TestUsedByNestedFunctionAfterTransformation()
		{
			return ReadonlyTest(nameof(UsedByNestedFunction), p => p
				.ConfigureAnalyzation(a => a
					.TypeConversion(symbol => TypeConversion.NewType)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(UsedByNestedFunction))));
					})
				)
			);
		}
	}
}
