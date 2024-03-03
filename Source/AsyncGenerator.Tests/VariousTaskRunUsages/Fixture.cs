using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestAfterAnalyzation()
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

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(10));

				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);
				IBodyFunctionReferenceAnalyzationResult methodReference;
				var awaitableMethods = new[]
				{
					waitActionTask, runSynchronouslyActionTask, waitFunctionTask, waitFunctionTaskNoResult, awaitedActionTask, awaitedFunctionTask, configuratedAwaitedActionTask, configuratedAwaitedFunctionTask
				};
				foreach (var awaitableMethod in awaitableMethods)
				{
					Assert.That(methods[awaitableMethod].BodyFunctionReferences.Count(), Is.EqualTo(1));
					methodReference = methods[awaitableMethod].BodyFunctionReferences.First();
					Assert.That(methodReference.AwaitInvocation, Is.True, awaitableMethod);
					Assert.That(methodReference.SynchronouslyAwaited, Is.True, awaitableMethod);
				}

				var notAwaitableMethods = new[]
				{
					notAwaitedActionTask, notAwaitedFunctionTask
				};
				foreach (var notAwaitableMethod in notAwaitableMethods)
				{
					Assert.That(methods[notAwaitableMethod].BodyFunctionReferences.Count(), Is.EqualTo(1));
					methodReference = methods[notAwaitableMethod].BodyFunctionReferences.First();
					Assert.That(methodReference.AwaitInvocation, Is.False);
					Assert.That(methodReference.SynchronouslyAwaited, Is.False);
				}

				var configurableAwaitableMethods = new[]
				{
					configuratedAwaitedActionTask, configuratedAwaitedFunctionTask,
				};
				foreach (var configurableAwaitableMethod in configurableAwaitableMethods)
				{
					methodReference = methods[configurableAwaitableMethod].BodyFunctionReferences.First();
					Assert.That(methodReference.ConfigureAwaitParameter, Is.Not.Null);
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.ToAsync;
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}
	}
}
