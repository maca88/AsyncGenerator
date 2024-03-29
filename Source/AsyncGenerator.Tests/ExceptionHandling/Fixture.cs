﻿using System;
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
		#region CatchProperty

		[Test]
		public Task TestCatchPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(CatchProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => symbol.Name == "get_IsValid"))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterCatchPropertyTransfromation)
				)
			);
		}

		[Test]
		public Task TestYamlCatchPropertyAfterTransformation()
		{
			return YamlReadonlyTest(nameof(CatchProperty),
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
					.ConfigureTransformation(t => t.AfterTransformation(AfterCatchPropertyTransfromation))
			);
		}

		[Test]
		public Task TestXmlCatchPropertyAfterTransformation()
		{
			return XmlReadonlyTest(nameof(CatchProperty),
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
					 .AfterTransformation(AfterCatchPropertyTransfromation))
			);
		}

		private void AfterCatchPropertyTransfromation(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.That(result.Documents.Count, Is.EqualTo(1));
			var document = result.Documents[0];
			Assert.That(document.OriginalModified, Is.Not.Null);
			Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(CatchProperty))));
		}

		#endregion

		#region NoCatchMethod

		[Test]
		public Task TestNoCatchMethodAfterTransformation()
		{
			return ReadonlyTest(nameof(NoCatchMethod), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ExceptionHandling(e => e
						.CatchMethodBody(symbol => symbol.Name == "Test" ? false : (bool?)null))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterNoCatchMethodTransfromation)
				)
			);
		}

		[Test]
		public Task TestYamlNoCatchMethodAfterTransformation()
		{
			return YamlReadonlyTest(nameof(NoCatchMethod),
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    exceptionHandling:
      catchMethodBody:
      - name: Test
        result: false
",
				p => p
					.ConfigureTransformation(t => t.AfterTransformation(AfterNoCatchMethodTransfromation))
			);
		}

		[Test]
		public Task TestXmlNoCatchMethodAfterTransformation()
		{
			return XmlReadonlyTest(nameof(NoCatchMethod),
				@"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Projects>
    <Project filePath=""AsyncGenerator.Tests.csproj"">
      <Analyzation>
        <MethodConversion>
          <Method conversion=""Smart"" all=""true"" />
        </MethodConversion>
        <ExceptionHandling>
          <CatchMethodBody>
            <Method name=""Test"" result=""false""/>
          </CatchMethodBody>
        </ExceptionHandling>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
",
				p => p
					.ConfigureTransformation(t => t
						.AfterTransformation(AfterNoCatchMethodTransfromation))
			);
		}

		private void AfterNoCatchMethodTransfromation(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.That(result.Documents.Count, Is.EqualTo(1));
			var document = result.Documents[0];
			Assert.That(document.OriginalModified, Is.Not.Null);
			Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(NoCatchMethod))));
		}

		#endregion

		#region PropagateOperationCanceledException

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
			Assert.That(result.Documents.Count, Is.EqualTo(1));
			var document = result.Documents[0];
			Assert.That(document.OriginalModified, Is.Not.Null);
			Console.WriteLine(document.Transformed.ToFullString());
			Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(fileName)));
		}

		#endregion
	}
}
