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
