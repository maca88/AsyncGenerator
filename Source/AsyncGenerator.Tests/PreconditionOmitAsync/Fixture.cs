using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestAfterAnalyzation()
		{
			var preconditionReturn = GetMethodName(o => o.PreconditionReturn(null));
			var preconditionVoid = GetMethodName(o => o.PreconditionVoid(null));
			var preconditionToSplit = GetMethodName(o => TestCase.PreconditionToSplit(null));
			var syncPrecondition = GetMethodName(o => o.SyncPrecondition(null));
			var readFile = GetMethodName(o => o.ReadFile());
			var syncReadFile = GetMethodName(o => o.SyncReadFile());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(6));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				CheckMethodsConversion(methods.Values);

				Assert.That(methods[readFile].ReferencedBy.Count(), Is.EqualTo(1));

				var method = methods[preconditionReturn];
				Assert.That(method.Preconditions.Count, Is.EqualTo(1));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				var methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);
				Assert.That(methodReference.ReferenceFunction, Is.EqualTo(methods[readFile]));

				method = methods[preconditionVoid];
				Assert.That(method.Preconditions.Count, Is.EqualTo(1));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);
				Assert.That(methodReference.ReferenceFunction, Is.Null);

				method = methods[preconditionToSplit];
				Assert.That(method.Preconditions.Count, Is.EqualTo(1));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.True);
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.True);
				Assert.That(methodReference.UseAsReturnValue, Is.False);
				Assert.That(methodReference.LastInvocation, Is.False);
				Assert.That(methodReference.ReferenceFunction, Is.Null);

				method = methods[syncPrecondition];
				Assert.That(method.Preconditions.Count, Is.EqualTo(1));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.True);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.FunctionReferences.Count, Is.EqualTo(0));
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ? MethodConversion.Ignore : MethodConversion.ToAsync)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			var syncReadFile = GetMethodName(o => o.SyncReadFile());
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ? MethodConversion.Ignore : MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
					})
				)
			);
		}

		[Test]
		public Task TestLocalFunctionsAfterTransformation()
		{
			var syncReadFile = GetMethodName(o => o.SyncReadFile());
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReadFile ? MethodConversion.Ignore : MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseLocalFunctions")));
					})
				)
			);
		}
	}
}
