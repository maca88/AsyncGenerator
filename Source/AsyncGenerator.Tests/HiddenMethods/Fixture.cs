using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.HiddenMethods.Input;

namespace AsyncGenerator.Tests.HiddenMethods
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestFutureEnumerableAfterTransformation()
		{
			return ReadonlyTest(nameof(FutureEnumerable), p => p
				.ConfigureAnalyzation(a => a
					.ScanMethodBody(true)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(FutureEnumerable)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
