using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Tests.SimpleClassInheritance.Input;
using AsyncGenerator.Tests.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleClassInheritance
{
	[TestFixture]
	public class Fixture : BaseTest<TestCase>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var read = GetMethodName(o => o.Read);
			var readFile = GetMethodName<DerivedClass>(o => o.ReadFile);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types.Count);
				var types = result.Documents[0].Namespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, types[nameof(DerivedClass)].Methods.Count);
				Assert.AreEqual(MethodConversion.ToAsync, types[nameof(DerivedClass)].Methods[0].Conversion);

				Assert.AreEqual(1, types[nameof(BaseClass)].Methods.Count);
				Assert.AreEqual(MethodConversion.ToAsync, types[nameof(BaseClass)].Methods[0].Conversion);


			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
					.Callbacks(c => c
						.AfterAnalyzation(afterAnalyzationFn)
					)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
