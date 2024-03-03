using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Dynamic.Input;

namespace AsyncGenerator.Tests.Dynamic
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestTypeArgumentAfterTransformation()
		{
			return ReadonlyTest(nameof(TypeArgument), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TypeArgument))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}
	}
}
