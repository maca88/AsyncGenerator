using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Events.Input;

namespace AsyncGenerator.Tests.Events
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAddRemoveEventAfterTransformation()
		{
			return ReadonlyTest(nameof(AddRemoveEvent),p => p
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
						Assert.AreEqual(GetOutputFile(nameof(AddRemoveEvent)), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
