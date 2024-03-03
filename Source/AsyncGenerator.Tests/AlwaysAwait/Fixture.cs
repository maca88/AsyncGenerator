using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Cref.Input;

namespace AsyncGenerator.Tests.AlwaysAwait
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		#region All methods

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AlwaysAwait(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterTransformation)
				)
			);
		}

		[Test]
		public Task TestYamlNoCatchMethodAfterTransformation()
		{
			return YamlReadonlyTest(nameof(TestCase),
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    alwaysAwait:
    - all: true
",
				p => p
					.ConfigureTransformation(t => t.AfterTransformation(AfterTransformation))
			);
		}

		[Test]
		public Task TestXmlNoCatchMethodAfterTransformation()
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
        <AlwaysAwait>
          <Method all=""true"" />
        </AlwaysAwait>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(AfterTransformation))
			);
		}

		#endregion

		#region Void methods

		[Test]
		public Task TestVoidAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AlwaysAwait(symbol => symbol.ReturnsVoid)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(VoidAfterTransformation)
				)
			);
		}

		[Test]
		public Task TestVoidYamlNoCatchMethodAfterTransformation()
		{
			return YamlReadonlyTest(nameof(TestCase),
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    alwaysAwait:
    - returnsVoid: true
",
				p => p
					.ConfigureTransformation(t => t.AfterTransformation(VoidAfterTransformation))
			);
		}

		[Test]
		public Task TestVoidXmlNoCatchMethodAfterTransformation()
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
        <AlwaysAwait>
          <Method returnsVoid=""true"" />
        </AlwaysAwait>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(VoidAfterTransformation))
			);
		}

		#endregion


		private void AfterTransformation(IProjectTransformationResult result)
		{
			AfterTransformation(result, nameof(TestCase));
		}

		private void VoidAfterTransformation(IProjectTransformationResult result)
		{
			AfterTransformation(result, nameof(TestCase) + "Void");
		}

		private void AfterTransformation(IProjectTransformationResult result, string fileName)
		{
			AssertValidAnnotations(result);
			Assert.That(result.Documents.Count, Is.EqualTo(1));
			var document = result.Documents[0];
			Assert.That(document.OriginalModified, Is.Not.Null);
			Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(fileName)));
		}
	}
}
