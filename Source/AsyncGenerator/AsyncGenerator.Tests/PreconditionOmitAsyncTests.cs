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
	public class PreconditionOmitAsyncTests : BaseTest<PreconditionOmitAsync>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var preconditionReturn = GetMethodName(o => o.PreconditionReturn(null));
			var preconditionVoid = GetMethodName(o => o.PreconditionVoid(null));
			var preconditionToSplit = GetMethodName(o => o.PreconditionToSplit(null));
			var syncPrecondition = GetMethodName(o => o.SyncPrecondition(null));
			var readFile = GetMethodName(o => o.ReadFile());
			var syncReadFile = GetMethodName(o => o.SyncReadFile());

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(6, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				CheckMethodsConversion(methods.Values);

				Assert.AreEqual(1, methods[readFile].InvokedBy.Count);

				var method = methods[preconditionReturn];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				var methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync,  methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				Assert.AreEqual(methods[readFile], methodReference.ReferenceFunctionData);

				method = methods[preconditionVoid];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				Assert.IsNull(methodReference.ReferenceFunctionData);

				method = methods[preconditionToSplit];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsFalse(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsTrue(method.SplitTail);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UsedAsReturnValue);
				Assert.IsFalse(methodReference.LastInvocation);
				Assert.IsNull(methodReference.ReferenceFunctionData);

				method = methods[syncPrecondition];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsTrue(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.MethodReferences.Count);

			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == syncReadFile ?  MethodConversion.Ignore : MethodConversion.ToAsync;
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
