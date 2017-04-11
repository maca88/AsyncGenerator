using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class SimpleSkipAsyncTests : BaseTest<SimpleSkipAsync>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var simpleReturn = GetMethodName(o => o.SimpleReturn());
			var doubleCallReturn = GetMethodName(o => o.DoubleCallReturn());
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var readFile = GetMethodName(o => o.ReadFile());
			var write = GetMethodName(() => SimpleFile.Write(null));

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(5, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				IFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;

				method = methods[doubleCallReturn];
				Assert.AreEqual(2, method.MethodReferences.Count);
				var methodReferences = method.MethodReferences.ToLookup(o => o.ReferenceSymbol.Name);
				methodReference = methodReferences[write].First();
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);

				methodReference = methodReferences[readFile].First();
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UsedAsReturnValue);

				method = methods[simpleReturn];
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences[0];
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
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
