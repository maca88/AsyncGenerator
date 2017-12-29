using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Diagnostics.Input;

namespace AsyncGenerator.Tests.Diagnostics
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			bool val;
			var test = GetMethodName(o => o.Test(out val));
			var test2 = GetMethodName(o => o.Test2(out val));
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
						var testMethod = (MethodData) methods[test];
						Assert.AreEqual(DiagnosticSeverity.Warning, testMethod.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(1, testMethod.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, testMethod.GetDiagnostics().First().DiagnosticSeverity);

						var test2Method = (MethodData)methods[test2];
						Assert.AreEqual(DiagnosticSeverity.Warning, test2Method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(1, test2Method.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, test2Method.GetDiagnostics().First().DiagnosticSeverity);
					})
				)
				.ConfigureTransformation(t => t.Disable())
			);
		}

		[Test]
		public Task IgnoreTest2YamlAfterAnalyzation()
		{
			return YamlReadonlyTest(
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    diagnostics:
      diagnoseMethod:
      - name: Test2
        result: false
",
				p => p
					.ConfigureAnalyzation(a => a
						.AfterAnalyzation(IgnoreTest2))
					.ConfigureTransformation(t => t.Disable())
			);
		}

		[Test]
		public Task IgnoreTest2XmlAfterAnalyzation()
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
        <Diagnostics>
          <DiagnoseMethod>
            <Method name=""Test2"" result=""false""/>
          </DiagnoseMethod>
        </Diagnostics>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureAnalyzation(a => a
						.AfterAnalyzation(IgnoreTest2))
					.ConfigureTransformation(t => t.Disable())
			);
		}

		private void IgnoreTest2(IProjectAnalyzationResult result)
		{
			bool val;
			var test2 = GetMethodName(o => o.Test2(out val));

			var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
			var test2Method = (MethodData)methods[test2];
			Assert.AreEqual(DiagnosticSeverity.Hidden, test2Method.IgnoredReason.DiagnosticSeverity);
			Assert.AreEqual(0, test2Method.GetDiagnostics().Count());
		}
	}
}
