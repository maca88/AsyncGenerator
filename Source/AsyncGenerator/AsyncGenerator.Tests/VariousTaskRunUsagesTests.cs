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
	public class VariousTaskRunUsagesTests : BaseTest<VariousTaskRunUsages>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var notAwaitedActionTask = GetMethodName(o => o.NotAwaitedActionTask);
			var notAwaitedFunctionTask = GetMethodName(o => o.NotAwaitedFunctionTask);
			var waitActionTask = GetMethodName(o => o.WaitActionTask);
			var waitFunctionTask = GetMethodName(o => o.WaitFunctionTask);
			var waitFunctionTaskNoResult = GetMethodName(o => o.WaitFunctionTaskNoResult);
			var awaitedActionTask = GetMethodName(o => o.AwaitedActionTask);
			var awaitedFunctionTask = GetMethodName(o => o.AwaitedFunctionTask);
			var configuratedAwaitedActionTask = GetMethodName(o => o.ConfiguratedAwaitedActionTask);
			var configuratedAwaitedFunctionTask = GetMethodName(o => o.ConfiguratedAwaitedFunctionTask);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(9, result.Documents[0].Namespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);
				Assert.AreEqual(1, methods[notAwaitedActionTask].MethodReferences.Count);
				Assert.IsFalse(methods[notAwaitedActionTask].MethodReferences[0].CanBeAwaited);

				Assert.AreEqual(1, methods[notAwaitedFunctionTask].MethodReferences.Count);
				Assert.IsFalse(methods[notAwaitedFunctionTask].MethodReferences[0].CanBeAwaited);
				//TODO: complete test
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.ToAsync;
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
