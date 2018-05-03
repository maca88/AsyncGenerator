using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using AsyncGenerator.Tests.Diagnostics.Input;

namespace AsyncGenerator.Tests.Diagnostics
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			bool val;
			var test = GetMethodName<TestCase>(o => o.Test(out val));
			var test2 = GetMethodName<TestCase>(o => o.Test2(out val));
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
						var testMethod = (MethodData)methods[test];
						Assert.AreEqual(DiagnosticSeverity.Warning, testMethod.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, testMethod.GetDiagnostics().Count());
						var bodyReference = testMethod.BodyFunctionReferences.First();
						Assert.AreEqual(1, bodyReference.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.GetDiagnostics().First().DiagnosticSeverity);

						var test2Method = (MethodData)methods[test2];
						Assert.AreEqual(DiagnosticSeverity.Warning, test2Method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, test2Method.GetDiagnostics().Count());
						bodyReference = testMethod.BodyFunctionReferences.First();
						Assert.AreEqual(1, bodyReference.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.GetDiagnostics().First().DiagnosticSeverity);
					})
				)
				.ConfigureTransformation(t => t.Disable())
			);
		}

		[Test]
		public Task IgnoreTest2YamlAfterAnalyzation()
		{
			return YamlReadonlyTest(nameof(TestCase),
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
			return XmlReadonlyTest(nameof(TestCase),
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
			var test2 = GetMethodName<TestCase>(o => o.Test2(out val));

			var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
			var test2Method = (MethodData)methods[test2];
			Assert.AreEqual(DiagnosticSeverity.Hidden, test2Method.IgnoredReason.DiagnosticSeverity);
			Assert.AreEqual(0, test2Method.GetDiagnostics().Count());
			var bodyReference = test2Method.BodyFunctionReferences.First();
			Assert.AreEqual(0, bodyReference.GetDiagnostics().Count());
		}



		[Test]
		public Task TestEnumerableWarningAfterAnalyzation()
		{
			var enumerable = GetMethodName<EnumerableWarning>(o => o.Enumerable);
			var enumerableQueryExpression = GetMethodName<EnumerableWarning>(o => o.EnumerableQueryExpression);
			var enumerableQueryExpressionNested = GetMethodName<EnumerableWarning>(o => o.EnumerableQueryExpressionNested);

			return ReadonlyTest(nameof(EnumerableWarning), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
						FunctionData method = (MethodData)methods[enumerable];
						Assert.AreEqual(DiagnosticSeverity.Warning, method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, method.GetDiagnostics().Count());
						Assert.AreEqual(2, method.ChildFunctions.Count);

						foreach (var functionData in method.ChildFunctions.Values)
						{
							Assert.AreEqual(DiagnosticSeverity.Warning, functionData.IgnoredReason.DiagnosticSeverity);
							Assert.AreEqual(0, functionData.GetDiagnostics().Count());
							Assert.AreEqual(1, functionData.BodyFunctionReferences.Count());
							var bodyReference = functionData.BodyFunctionReferences.First();
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.IgnoredReason.DiagnosticSeverity);
							Assert.AreEqual(1, bodyReference.GetDiagnostics().Count());
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.GetDiagnostics().First().DiagnosticSeverity);
						}


						method = (MethodData)methods[enumerableQueryExpression];
						Assert.AreEqual(DiagnosticSeverity.Warning, method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, method.GetDiagnostics().Count());

						Assert.AreEqual(2, method.BodyFunctionReferences.Count());
						foreach (var bodyReference in method.BodyFunctionReferences)
						{
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.IgnoredReason.DiagnosticSeverity);
							Assert.AreEqual(1, bodyReference.GetDiagnostics().Count());
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.GetDiagnostics().First().DiagnosticSeverity);
						}

						method = (MethodData)methods[enumerableQueryExpressionNested];
						Assert.AreEqual(DiagnosticSeverity.Warning, method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, method.GetDiagnostics().Count());
						Assert.AreEqual(1, method.ChildFunctions.Count);

						method = method.ChildFunctions.First().Value;
						Assert.AreEqual(DiagnosticSeverity.Warning, method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(0, method.GetDiagnostics().Count());

						Assert.AreEqual(2, method.BodyFunctionReferences.Count());
						foreach (var bodyReference in method.BodyFunctionReferences)
						{
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.IgnoredReason.DiagnosticSeverity);
							Assert.AreEqual(1, bodyReference.GetDiagnostics().Count());
							Assert.AreEqual(DiagnosticSeverity.Warning, bodyReference.GetDiagnostics().First().DiagnosticSeverity);
						}
					})
				)
				.ConfigureTransformation(t => t.Disable())
			);
		}
	}
}
