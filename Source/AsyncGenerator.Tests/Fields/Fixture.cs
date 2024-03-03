using System.Threading.Tasks;
using AsyncGenerator.Core;
using NUnit.Framework;
using AsyncGenerator.Tests.Fields.Input;

namespace AsyncGenerator.Tests.Fields
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestArgumentNewTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(Argument), p => p
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
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile($"{nameof(Argument)}NewType")));
					})
				)
			);
		}
	}
}
