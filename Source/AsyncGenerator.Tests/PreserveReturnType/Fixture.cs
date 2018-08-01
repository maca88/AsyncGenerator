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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(LastStatement)), document.Transformed.ToFullString());
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
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(FunctionArgument)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
