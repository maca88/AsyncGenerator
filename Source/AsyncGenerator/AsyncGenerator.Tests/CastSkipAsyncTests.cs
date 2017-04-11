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
	public class CastSkipAsyncTests : BaseTest<CastSkipAsync>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var longCastReturn = GetMethodName(o => o.LongCastReturn());
			var enumerableCastReturn = GetMethodName(o => o.EnumerableCastReturn());
			var noCastReturn = GetMethodName(o => o.NoCastReturn());
			var noCastReturnTask = GetMethodName(o => o.NoCastReturnTask());

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(6, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				var awaitRequiredMethods = new[]
				{
					longCastReturn,
					enumerableCastReturn
				};

				IFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;
				foreach (var awaitRequiredMethod in awaitRequiredMethods)
				{
					method = methods[awaitRequiredMethod];
					Assert.AreEqual(1, method.MethodReferences.Count);
					methodReference = method.MethodReferences[0];
					Assert.IsTrue(methodReference.AwaitInvocation);
					Assert.IsTrue(methodReference.UsedAsReturnValue);
				}

				var awaitNotRequiredMethods = new[]
				{
					noCastReturn,
					noCastReturnTask
				};
				foreach (var awaitNotRequiredMethod in awaitNotRequiredMethods)
				{
					method = methods[awaitNotRequiredMethod];
					Assert.AreEqual(1, method.MethodReferences.Count);
					methodReference = method.MethodReferences[0];
					Assert.IsFalse(methodReference.AwaitInvocation);
					Assert.IsTrue(methodReference.UsedAsReturnValue);
				}
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
