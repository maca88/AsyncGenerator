using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Transformation;
using NUnit.Framework;
using AsyncGenerator.Tests.Cref.Input;

namespace AsyncGenerator.Tests.MethodGeneration
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.MethodGeneration(m => m.Name == "Read" ? Core.MethodGeneration.Ignore : Core.MethodGeneration.Generate)
					.AfterTransformation(ValidateDocument)
				)
			);
		}

		[Test]
		public Task TestYamlAfterTransformation()
		{
			return YamlReadonlyTest(nameof(TestCase),
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
  transformation:
    methodGeneration:
    - generation: Ignore
      name: Read
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(ValidateDocument))
			);
		}

		[Test]
		public Task TestXmlAfterTransformation()
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
      </Analyzation>
      <Transformation>
        <MethodGeneration>
          <Method generation=""Ignore"" name=""Read"" />
        </MethodGeneration>
      </Transformation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(ValidateDocument))
			);
		}

		private void ValidateDocument(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
		}
	}
}
