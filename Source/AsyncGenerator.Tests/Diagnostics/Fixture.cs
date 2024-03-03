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
						Assert.That(testMethod.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(testMethod.GetDiagnostics().Count(), Is.EqualTo(0));
						var bodyReference = testMethod.BodyFunctionReferences.First();
						Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(1));
						Assert.That(bodyReference.GetDiagnostics().First().DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));

						var test2Method = (MethodData)methods[test2];
						Assert.That(test2Method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(test2Method.GetDiagnostics().Count(), Is.EqualTo(0));
						bodyReference = testMethod.BodyFunctionReferences.First();
						Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(1));
						Assert.That(bodyReference.GetDiagnostics().First().DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
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
			Assert.That(test2Method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Hidden));
			Assert.That(test2Method.GetDiagnostics().Count(), Is.EqualTo(0));
			var bodyReference = test2Method.BodyFunctionReferences.First();
			Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(0));
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
						Assert.That(method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(method.GetDiagnostics().Count(), Is.EqualTo(0));
						Assert.That(method.ChildFunctions.Count, Is.EqualTo(2));

						foreach (var functionData in method.ChildFunctions.Values)
						{
							Assert.That(functionData.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
							Assert.That(functionData.GetDiagnostics().Count(), Is.EqualTo(0));
							Assert.That(functionData.BodyFunctionReferences.Count(), Is.EqualTo(1));
							var bodyReference = functionData.BodyFunctionReferences.First();
							Assert.That(bodyReference.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
							Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(1));
							Assert.That(bodyReference.GetDiagnostics().First().DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						}


						method = (MethodData)methods[enumerableQueryExpression];
						Assert.That(method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(method.GetDiagnostics().Count(), Is.EqualTo(0));

						Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(2));
						foreach (var bodyReference in method.BodyFunctionReferences)
						{
							Assert.That(bodyReference.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
							Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(1));
							Assert.That(bodyReference.GetDiagnostics().First().DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						}

						method = (MethodData)methods[enumerableQueryExpressionNested];
						Assert.That(method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(method.GetDiagnostics().Count(), Is.EqualTo(0));
						Assert.That(method.ChildFunctions.Count, Is.EqualTo(1));

						method = method.ChildFunctions.First().Value;
						Assert.That(method.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						Assert.That(method.GetDiagnostics().Count(), Is.EqualTo(0));

						Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(2));
						foreach (var bodyReference in method.BodyFunctionReferences)
						{
							Assert.That(bodyReference.IgnoredReason.DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
							Assert.That(bodyReference.GetDiagnostics().Count(), Is.EqualTo(1));
							Assert.That(bodyReference.GetDiagnostics().First().DiagnosticSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
						}
					})
				)
				.ConfigureTransformation(t => t.Disable())
			);
		}
	}
}
