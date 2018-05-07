using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.ExceptionHandling.Input;
using System.Threading;

namespace AsyncGenerator.Tests.ExceptionHandling
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(nameof(TestCase), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => symbol.Name == "get_IsValid"))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterTransfromation)
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
    exceptionHandling:
      catchPropertyGetterCalls:
      - name: get_IsValid
        result: true
",
				p => p
					.ConfigureTransformation(t => t.AfterTransformation(AfterTransfromation))
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
        <ExceptionHandling>
          <CatchPropertyGetterCalls>
            <Method name=""get_IsValid"" result=""true""/>
          </CatchPropertyGetterCalls>
        </ExceptionHandling>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
					 .AfterTransformation(AfterTransfromation))
			);
		}

		private void AfterTransfromation(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
		}

		[Test]
		public Task TestOperationCanceledExceptionPropagationAfterTransformation()
		{
			return ReadonlyTest(nameof(PropagateOperationCanceledException), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo => MethodCancellationToken.Required))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(r => AfterPropagateOperationCanceledExceptionTransfromation(r, nameof(PropagateOperationCanceledException)))
				)
			);
		}

		[Test]
		public Task TestDoNotPropagateOperationCanceledExceptionAfterTransformation()
		{
			return ReadonlyTest(nameof(DoNotPropagateOperationCanceledException), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(t => t
						.ParameterGeneration(symbolInfo => MethodCancellationToken.Required))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(r => AfterPropagateOperationCanceledExceptionTransfromation(r, nameof(DoNotPropagateOperationCanceledException)))
				)
			);
		}

		private void AfterPropagateOperationCanceledExceptionTransfromation(IProjectTransformationResult result, string fileName)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Console.WriteLine(document.Transformed.ToFullString());
			Assert.AreEqual(GetOutputFile(fileName), document.Transformed.ToFullString());
		}
	}
}
