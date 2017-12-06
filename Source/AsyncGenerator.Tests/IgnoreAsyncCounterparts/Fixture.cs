using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.IgnoreAsyncCounterparts.Input;

namespace AsyncGenerator.Tests.IgnoreAsyncCounterparts
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
					.IgnoreAsyncCounterparts(symbol => symbol.Name == "ReadAsync")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		[Test]
		public Task TestYamlAfterTransformation()
		{
			return YamlReadonlyTest(
@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    ignoreAsyncCounterparts:
    - name: ReadAsync
",
				p => p
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}

		[Test]
		public Task TestXmlAfterTransformation()
		{
			return XmlReadonlyTest(
				@"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Projects>
    <Project filePath=""AsyncGenerator.Tests.csproj"">
      <Analyzation>
        <MethodConversion>
          <Method conversion=""Smart"" all=""true"" />
        </MethodConversion>
        <IgnoreAsyncCounterparts>
          <Method name=""ReadAsync"" />
        </IgnoreAsyncCounterparts>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(result =>
						{
							Assert.AreEqual(0, result.Documents.Count);
						})
					)
			);
		}
	}
}
