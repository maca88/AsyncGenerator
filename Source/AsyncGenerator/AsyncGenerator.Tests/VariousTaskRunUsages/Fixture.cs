using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using NUnit.Framework;

namespace AsyncGenerator.Tests.VariousTaskRunUsages
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.TestCase>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var notAwaitedActionTask = GetMethodName(o => o.NotAwaitedActionTask);
			var notAwaitedFunctionTask = GetMethodName(o => o.NotAwaitedFunctionTask);
			var waitActionTask = GetMethodName(o => o.WaitActionTask);
			var runSynchronouslyActionTask = GetMethodName(o => o.RunSynchronouslyActionTask);
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
				Assert.AreEqual(10, result.Documents[0].Namespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);
				IBodyFunctionReferenceAnalyzationResult methodReference;
				var awaitableMethods = new[]
				{
					waitActionTask,
					runSynchronouslyActionTask,
					waitFunctionTask,
					waitFunctionTaskNoResult,
					awaitedActionTask,
					awaitedFunctionTask,
					configuratedAwaitedActionTask,
					configuratedAwaitedFunctionTask
				};
				foreach (var awaitableMethod in awaitableMethods)
				{
					Assert.AreEqual(1, methods[awaitableMethod].MethodReferences.Count);
					methodReference = methods[awaitableMethod].MethodReferences[0];
					Assert.IsTrue(methodReference.AwaitInvocation);
					Assert.IsTrue(methodReference.SynchronouslyAwaited);
				}

				var notAwaitableMethods = new[]
				{
					notAwaitedActionTask,
					notAwaitedFunctionTask
				};
				foreach (var notAwaitableMethod in notAwaitableMethods)
				{
					Assert.AreEqual(1, methods[notAwaitableMethod].MethodReferences.Count);
					methodReference = methods[notAwaitableMethod].MethodReferences[0];
					Assert.IsFalse(methodReference.AwaitInvocation);
					Assert.IsFalse(methodReference.SynchronouslyAwaited);
				}

				var configurableAwaitableMethods = new[]
				{
					configuratedAwaitedActionTask,
					configuratedAwaitedFunctionTask,
				};
				foreach (var configurableAwaitableMethod in configurableAwaitableMethods)
				{
					methodReference = methods[configurableAwaitableMethod].MethodReferences[0];
					Assert.IsNotNull(methodReference.ConfigureAwaitParameter);
				}
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
