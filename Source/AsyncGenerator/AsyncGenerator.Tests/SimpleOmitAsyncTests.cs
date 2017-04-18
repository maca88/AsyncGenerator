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
	public class SimpleOmitAsyncTests : BaseTest<SimpleOmitAsync>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var simpleReturn = GetMethodName(o => o.SimpleReturn());
			var doubleCallReturn = GetMethodName(o => o.DoubleCallReturn());
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var simpleVoid = GetMethodName(o => o.SimpleVoid);
			var doubleCallVoid = GetMethodName(o => o.DoubleCallVoid);
			var expressionVoid = GetMethodName(o => o.ExpressionVoid);
			var expressionReturn = GetMethodName(o => o.ExpressionReturn());
			var readFile = GetMethodName(o => o.ReadFile());
			var syncReadFile = GetMethodName(o => o.SyncReadFile());
			var write = GetMethodName(() => SimpleFile.Write(null));
			var read = GetMethodName(() => SimpleFile.Read());

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(9, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				IInvokeFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;

				method = methods[simpleReturn];
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences[0];
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);

				method = methods[doubleCallReturn];
				Assert.IsFalse(method.OmitAsync);
				Assert.IsFalse(method.SplitTail);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.AreEqual(2, method.MethodReferences.Count);
				var methodReferences = method.MethodReferences.ToLookup(o => o.ReferenceSymbol.Name);
				methodReference = methodReferences[write].First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				methodReference = methodReferences[readFile].First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UsedAsReturnValue);
				Assert.IsFalse(methodReference.LastInvocation);

				method = methods[syncReturn];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsTrue(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.Ignore, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);

				method = methods[simpleVoid];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);

				method = methods[doubleCallVoid];
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.SplitTail);
				Assert.IsTrue(method.WrapInTryCatch);
				Assert.AreEqual(2, method.MethodReferences.Count);
				methodReferences = method.MethodReferences.ToLookup(o => o.ReferenceSymbol.Name);
				methodReference = methodReferences[read].First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				methodReference = methodReferences[syncReadFile].First();
				Assert.AreEqual(ReferenceConversion.Ignore, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UsedAsReturnValue);
				Assert.IsFalse(methodReference.LastInvocation);

				method = methods[expressionVoid];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);

				method = methods[expressionReturn];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == syncReturn ? MethodConversion.ToAsync : MethodConversion.Smart;
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
