using System;
using System.Linq;
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
		public void TestAfterAnalyzation()
		{
			var longCastReturn = GetMethodName(o => o.LongCastReturn());
			var enumerableCastReturn = GetMethodName(o => o.EnumerableCastReturn());
			var noCastReturn = GetMethodName(o => o.NoCastReturn());
			var noCastReturnTask = GetMethodName(o => o.NoCastReturnTask());

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(6, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
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
					Assert.IsFalse(method.OmitAsync);
					Assert.AreEqual(1, method.BodyFunctionReferences.Count());
					methodReference = method.BodyFunctionReferences.First();
					Assert.IsTrue(methodReference.AwaitInvocation);
					Assert.IsTrue(methodReference.UseAsReturnValue);
				}

				var awaitNotRequiredMethods = new[]
				{
					noCastReturn, noCastReturnTask
				};
				foreach (var awaitNotRequiredMethod in awaitNotRequiredMethods)
				{
					method = methods[awaitNotRequiredMethod];
					Assert.IsTrue(method.OmitAsync);
					Assert.AreEqual(1, method.BodyFunctionReferences.Count());
					methodReference = method.BodyFunctionReferences.First();
					Assert.IsFalse(methodReference.AwaitInvocation);
					Assert.IsTrue(methodReference.UseAsReturnValue);
				}
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
