using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.ConfigurationOverride.Input;

namespace AsyncGenerator.Tests.ConfigurationOverride
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAfterTransformation()
		{
			var copy = GetMethodName<PartialCopy>(o => o.Copy);
			return ReadonlyTest(nameof(PartialCopy), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == copy ? MethodConversion.Copy : MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(PartialCopy))));
					})
				)
			);
		}
	}
}
