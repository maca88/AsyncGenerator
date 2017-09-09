using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Tests.Github.Issue50.Input;
using AsyncGenerator.Tests.NewTypes.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Github.Issue50
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test, Repeat(10)]
		public void TestAfterTransformation()
		{
			var read = GetMethodName(o => o.Read);
			var config = Configure(p => p
				.ConcurrentRun()
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(IInterface) && symbol.Name == read ? MethodConversion.Ignore : MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(3, result.Documents.Count);
						var documents = result.Documents.ToDictionary(o => o.AnalyzationResult.Document.Name.Split('.')[0]);
						var names = new []
						{
							nameof(IInterface),
							nameof(TestCase),
							nameof(Abstract)
						};
						foreach (var name in names)
						{
							var document = documents[name];
							var readMethod = document.AnalyzationResult.GetAllTypes()
								.SelectMany(o => o.Methods.Where(m => m.Symbol.Name == read))
								.Single();
							Assert.AreEqual(2, readMethod.RelatedMethods.Count);

							Assert.IsNotNull(document.OriginalModified);
							Assert.AreEqual(GetOutputFile(name), document.Transformed.ToFullString(), name);
						}
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
