using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using NUnit.Framework;

namespace AsyncGenerator.Tests.CastOmitAsync
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var longCastReturn = GetMethodName(o => o.LongCastReturn());
			var enumerableCastReturn = GetMethodName(o => o.EnumerableCastReturn());
			var noCastReturn = GetMethodName(o => o.NoCastReturn());
			var noCastReturnTask = GetMethodName(o => o.NoCastReturnTask());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(6));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				var awaitRequiredMethods = new[]
				{
					longCastReturn, enumerableCastReturn
				};

				IBodyFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;
				foreach (var awaitRequiredMethod in awaitRequiredMethods)
				{
					method = methods[awaitRequiredMethod];
					Assert.That(method.OmitAsync, Is.False);
					Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
					methodReference = method.BodyFunctionReferences.First();
					Assert.That(methodReference.AwaitInvocation, Is.True);
					Assert.That(methodReference.UseAsReturnValue, Is.True);
				}

				var awaitNotRequiredMethods = new[]
				{
					noCastReturn, noCastReturnTask
				};
				foreach (var awaitNotRequiredMethod in awaitNotRequiredMethods)
				{
					method = methods[awaitNotRequiredMethod];
					Assert.That(method.OmitAsync, Is.True);
					Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
					methodReference = method.BodyFunctionReferences.First();
					Assert.That(methodReference.AwaitInvocation, Is.False);
					Assert.That(methodReference.UseAsReturnValue, Is.True);
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}
	}
}
