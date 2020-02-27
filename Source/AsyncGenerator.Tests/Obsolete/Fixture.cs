using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Obsolete.Input;
using AsyncGenerator.Core.Plugins;

namespace AsyncGenerator.Tests.Obsolete
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestObsoleteAsyncAfterTransformation()
		{
			return ReadonlyTest(nameof(ObsoleteTest), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.TypeConversion(symbol => TypeConversion.NewType)
					.ScanMethodBody(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(ObsoleteTest)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
