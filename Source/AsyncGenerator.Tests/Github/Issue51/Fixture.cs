using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Github.Issue51.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Github.Issue51
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test, Repeat(5)]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInterface) && symbol.Name == "Read" ? MethodConversion.Ignore : MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						foreach (var method in document.AnalyzationResult.AllTypes.SelectMany(o => o.Methods))
						{
							Assert.That(method.CancellationTokenRequired, Is.False, method.Symbol.ToString());
							foreach (var reference in method.BodyFunctionReferences)
							{
								Assert.That(reference.PassCancellationToken, Is.False, reference.ReferenceLocation.Location.ToString());
							}
						}

						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
					})
				)
			);
		}
	}
}
