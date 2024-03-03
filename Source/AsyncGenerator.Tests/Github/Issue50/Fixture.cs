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
		public Task TestAfterTransformation()
		{
			var read = GetMethodName(o => o.Read);
			return ReadonlyTest(p => p
				.ConcurrentRun()
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.ContainingType.Name == nameof(IInterface) && symbol.Name == read ? MethodConversion.Ignore : MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(3));
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
							var readMethod = document.AnalyzationResult.AllTypes
								.SelectMany(o => o.Methods.Where(m => m.Symbol.Name == read))
								.Single();
							Assert.That(readMethod.RelatedMethods.Count, Is.EqualTo(2));

							Assert.That(document.OriginalModified, Is.Not.Null);
							Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(name)), name);
						}
					})
				)
			);
		}
	}
}
