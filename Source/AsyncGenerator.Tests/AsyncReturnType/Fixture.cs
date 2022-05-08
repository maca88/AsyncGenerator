#if NETCOREAPP2_1_OR_GREATER
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Transformation;
using NUnit.Framework;
using AsyncGenerator.Tests.AsyncReturnType.Input;
using TestCase = AsyncGenerator.Tests.Cref.Input.TestCase;

namespace AsyncGenerator.Tests.AsyncReturnType
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
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
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestNullCoalescingAfterTransformation()
		{
			return ReadonlyTest(nameof(NullCoalescing), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(NullCoalescing)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestValueTaskReturnTypeAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AsyncReturnType(symbol => Core.AsyncReturnType.ValueTask)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(ValidateValueTask)
				)
			);
		}

		[Test]
		public Task TestValueTaskReturnTypeYamlAfterTransformation()
		{
			return YamlReadonlyTest(nameof(TestCase),
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    asyncReturnType:
    - returnType: ValueTask
      all: true
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(ValidateValueTask))
			);
		}

		[Test]
		public Task TestValueTaskReturnTypeXmlAfterTransformation()
		{
			return XmlReadonlyTest(nameof(TestCase),
				@"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Projects>
    <Project filePath=""AsyncGenerator.Tests.csproj"">
      <Analyzation>
        <MethodConversion>
          <Method conversion=""Smart"" all=""true"" />
        </MethodConversion>
        <AsyncReturnType>
          <Method returnType=""ValueTask"" all=""true"" />
        </AsyncReturnType>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(ValidateValueTask))
			);
		}

		private void ValidateValueTask(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Assert.AreEqual(GetOutputFile(nameof(TestCase) + "ValueTask"), document.Transformed.ToFullString());
		}
	}
}
#endif
