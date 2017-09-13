using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
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

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(10, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);
				IBodyFunctionReferenceAnalyzationResult methodReference;
				var awaitableMethods = new[]
				{
					waitActionTask, runSynchronouslyActionTask, waitFunctionTask, waitFunctionTaskNoResult, awaitedActionTask, awaitedFunctionTask, configuratedAwaitedActionTask, configuratedAwaitedFunctionTask
				};
				foreach (var awaitableMethod in awaitableMethods)
				{
					Assert.AreEqual(1, methods[awaitableMethod].BodyFunctionReferences.Count());
					methodReference = methods[awaitableMethod].BodyFunctionReferences.First();
					Assert.IsTrue(methodReference.AwaitInvocation, awaitableMethod);
					Assert.IsTrue(methodReference.SynchronouslyAwaited, awaitableMethod);
				}

				var notAwaitableMethods = new[]
				{
					notAwaitedActionTask, notAwaitedFunctionTask
				};
				foreach (var notAwaitableMethod in notAwaitableMethods)
				{
					Assert.AreEqual(1, methods[notAwaitableMethod].BodyFunctionReferences.Count());
					methodReference = methods[notAwaitableMethod].BodyFunctionReferences.First();
					Assert.IsFalse(methodReference.AwaitInvocation);
					Assert.IsFalse(methodReference.SynchronouslyAwaited);
				}

				var configurableAwaitableMethods = new[]
				{
					configuratedAwaitedActionTask, configuratedAwaitedFunctionTask,
				};
				foreach (var configurableAwaitableMethod in configurableAwaitableMethods)
				{
					methodReference = methods[configurableAwaitableMethod].BodyFunctionReferences.First();
					Assert.IsNotNull(methodReference.ConfigureAwaitParameter);
				}
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.ToAsync;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
