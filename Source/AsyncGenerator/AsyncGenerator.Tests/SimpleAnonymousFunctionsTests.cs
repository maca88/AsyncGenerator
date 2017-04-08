using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Tests.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class SimpleAnonymousFunctionsTests : BaseTest<SimpleAnonymousFunctions>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile());
			var declareAction = GetMethodName(o => o.DeclareAction());
			var declareFunction = GetMethodName(o => o.DeclareFunction());
			var declareNamedDelegate = GetMethodName(o => o.DeclareNamedDelegate());
			var returnDelegate = GetMethodName(o => o.ReturnDelegate());
			var argumentAction = GetMethodName(o => o.ArgumentAction);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(TypeConversion.Partial, result.Documents[0].Namespaces[0].Types[0].Conversion);
				Assert.AreEqual(6, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(5, methods[readFile].InvokedBy.Count);

				var readFileMethod = methods[readFile];

				var ignoredAnonymousMethods = new[]
				{
					declareNamedDelegate,
					returnDelegate,
					declareFunction,
					declareAction
				};
				IMethodAnalyzationResult method;
				foreach (var ignoredAnonymousMethod in ignoredAnonymousMethods)
				{
					method = methods[ignoredAnonymousMethod];
					Assert.AreEqual(MethodConversion.Ignore, method.Conversion);
					Assert.AreEqual(1, method.AnonymousFunctions.Count);
					Assert.AreEqual(MethodConversion.Ignore, method.AnonymousFunctions[0].Conversion);
					Assert.AreEqual(0, method.AnonymousFunctions[0].MethodReferences.Count);
					Assert.IsTrue(readFileMethod.InvokedBy.Any(o => o == method.AnonymousFunctions[0]));
				}

				method = methods[argumentAction];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.AreEqual(1, method.AnonymousFunctions.Count);
				Assert.AreEqual(MethodConversion.ToAsync, method.AnonymousFunctions[0].Conversion);
				Assert.AreEqual(1, method.AnonymousFunctions[0].MethodReferences.Count);
				Assert.IsTrue(readFileMethod.InvokedBy.Any(o => o == method.AnonymousFunctions[0]));
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Smart;
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
