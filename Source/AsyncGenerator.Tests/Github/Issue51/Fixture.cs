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
		public void TestAfterTransformation()
		{
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(ITestInterface) && symbol.Name == "Read" ? MethodConversion.Ignore : MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						foreach (var method in document.AnalyzationResult.AllTypes.SelectMany(o => o.Methods))
						{
							Assert.IsFalse(method.CancellationTokenRequired, method.Symbol.ToString());
							foreach (var reference in method.BodyFunctionReferences)
							{
								Assert.IsFalse(reference.PassCancellationToken, reference.ReferenceLocation.Location.ToString());
							}
						}

						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
