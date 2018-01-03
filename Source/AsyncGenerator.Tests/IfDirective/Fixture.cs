using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.AbstractClass.Input;
using AsyncGenerator.Tests.IfDirective.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.IfDirective
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
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCase"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(TryCatch), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TryCatch)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestTryCatchCallForwardingAfterTransformation()
		{
			return ReadonlyTest(nameof(TryCatch), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.CallForwarding(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TryCatch) + "ForwardCall"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
