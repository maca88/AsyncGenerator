using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NullableReferences
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public async Task TestNullableRestoreDirective()
		{
			var ignore = false;
			var config = AsyncCodeConfiguration.Create()
				.ConfigureProject(GetTestProjectPath("NullableRestore"), p => p
					.ConfigureAnalyzation(a => a
						.MethodConversion(s => MethodConversion.Smart)
						.CancellationTokens(true))
					.ConfigureTransformation(t => t
						.AfterTransformation(result =>
						{
							if (((CSharpCompilationOptions)result.Project.CompilationOptions).NullableContextOptions == NullableContextOptions.Disable)
							{
								ignore = true;
								return;
							}

							AssertValidAnnotations(result);
							Assert.That(result.Documents.Count, Is.EqualTo(1));
							var document = result.Documents[0];
							Assert.That(document.OriginalModified, Is.Not.Null);
							Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("NullableRestore")));
						}))
					.ApplyChanges(false));

			await AsyncCodeGenerator.GenerateAsync(config);
			if (ignore)
			{
				Assert.Ignore("Nullable is not enabled.");
			}
		}
	}
}