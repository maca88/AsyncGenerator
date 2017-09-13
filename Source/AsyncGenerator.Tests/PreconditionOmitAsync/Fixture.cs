using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using NUnit.Framework;
using AsyncGenerator.Tests.PreconditionOmitAsync.Input;

namespace AsyncGenerator.Tests.PreconditionOmitAsync
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var preconditionReturn = GetMethodName(o => o.PreconditionReturn(null));
			var preconditionVoid = GetMethodName(o => o.PreconditionVoid(null));
			var preconditionToSplit = GetMethodName(o => TestCase.PreconditionToSplit(null));
			var syncPrecondition = GetMethodName(o => o.SyncPrecondition(null));
			var readFile = GetMethodName(o => o.ReadFile());
			var syncReadFile = GetMethodName(o => o.SyncReadFile());

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(6, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				CheckMethodsConversion(methods.Values);

				Assert.AreEqual(1, methods[readFile].ReferencedBy.Count());

				var method = methods[preconditionReturn];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				var methodReference = method.BodyFunctionReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				Assert.AreEqual(methods[readFile], methodReference.ReferenceFunction);

				method = methods[preconditionVoid];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				methodReference = method.BodyFunctionReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				Assert.IsNull(methodReference.ReferenceFunction);

				method = methods[preconditionToSplit];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsTrue(method.SplitTail);
				methodReference = method.BodyFunctionReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UseAsReturnValue);
				Assert.IsFalse(methodReference.LastInvocation);
				Assert.IsNull(methodReference.ReferenceFunction);

				method = methods[syncPrecondition];
				Assert.AreEqual(1, method.Preconditions.Count);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsTrue(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.FunctionReferences.Count);
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ?  MethodConversion.Ignore : MethodConversion.ToAsync)
					.AfterAnalyzation(AfterAnalyzation)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestAfterTransformation()
		{
			var syncReadFile = GetMethodName(o => o.SyncReadFile());
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ? MethodConversion.Ignore : MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestLocalFunctionsAfterTransformation()
		{
			var syncReadFile = GetMethodName(o => o.SyncReadFile());
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ? MethodConversion.Ignore : MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseLocalFunctions"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
