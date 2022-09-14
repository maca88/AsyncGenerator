using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Transformation;
using NUnit.Framework;
using AsyncGenerator.Tests.Cref.Input;

namespace AsyncGenerator.Tests.PreprocessorDirectives
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
					.PreprocessorDirectives(d => d
						.AddForMethod(symbol => symbol.Name == "Read"
							? new Core.PreprocessorDirectives("#if TEST", "#endif")
							: new Core.PreprocessorDirectives("#region TEST", "#endregion")))
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
    preprocessorDirectives:
      addForMethod:
        - name: Read
          startDirective: ""#if TEST""
          endDirective: ""#endif""
        - name: Read2
          startDirective: ""#region TEST""
          endDirective: ""#endregion""
        - name: Read3
          startDirective: ""#region TEST""
          endDirective: ""#endregion""
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
        <PreprocessorDirectives>
          <AddForMethod>
            <Method name=""Read"">
              <StartDirective>#if TEST</StartDirective>
              <EndDirective>#endif</EndDirective>
            </Method>
            <Method name=""Read2"">
              <StartDirective>#region TEST</StartDirective>
              <EndDirective>#endregion</EndDirective>
            </Method>
            <Method name=""Read3"">
              <StartDirective>#region TEST</StartDirective>
              <EndDirective>#endregion</EndDirective>
            </Method>
          </AddForMethod>
        </PreprocessorDirectives>
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
