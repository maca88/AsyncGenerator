using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.PreserveReturnType.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.PreserveReturnType
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test, Repeat(10)]
		public Task TestLastStatementAfterTransformation()
		{
			return ReadonlyTest(nameof(LastStatement), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => symbol.Name == "Test")
					.AlwaysAwait(symbol => symbol.Name == "Test")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(LastStatement))));
					})
				)
			);
		}

		[Test]
		public Task TestFunctionArgumentAfterTransformation()
		{
			return ReadonlyTest(nameof(FunctionArgument), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(symbol => symbol.Name == "Test")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(FunctionArgument))));
					})
				)
			);
		}
	}
}
