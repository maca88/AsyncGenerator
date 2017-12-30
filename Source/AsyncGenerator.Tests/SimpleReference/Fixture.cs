using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.SimpleReference.Input;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleReference
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public async Task TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile);
			var callReadFile = GetMethodName(o => o.CallReadFile);
			var callCallReadFile = GetMethodName(o => o.CallCallReadFile);
			var read = GetMethodName(() => SimpleFile.Read());
			var readAsync = GetMethodName(() => SimpleFile.ReadAsync());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.IsTrue(methods[readFile].OmitAsync);

				// Check InvokedBy
				Assert.AreEqual(1, methods[readFile].ReferencedBy.Count());
				Assert.AreEqual(methods[callReadFile], methods[readFile].ReferencedBy.First());
				Assert.AreEqual(1, methods[callReadFile].ReferencedBy.Count());
				Assert.AreEqual(0, methods[callCallReadFile].ReferencedBy.Count());

				// Check MethodReferences
				Assert.AreEqual(1, methods[readFile].BodyFunctionReferences.Count());
				var methodReference = methods[readFile].BodyFunctionReferences.First();
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsNull(methodReference.ReferenceFunction);
				Assert.AreEqual(read, methodReference.ReferenceSymbol.Name);
				Assert.AreEqual(1, methodReference.ReferenceAsyncSymbols.Count);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[0].Name);

				// Check Conversion
				Assert.AreEqual(MethodConversion.ToAsync, methods[readFile].Conversion);
				Assert.AreEqual(MethodConversion.ToAsync, methods[callReadFile].Conversion);
				Assert.AreEqual(MethodConversion.ToAsync, methods[callCallReadFile].Conversion);
			}

			var methodConversions = new[] {MethodConversion.ToAsync, MethodConversion.Smart};
			foreach (var methodConversion in methodConversions)
			{
				await ReadonlyTest(p => p
					.ConfigureAnalyzation(a => a
						.MethodConversion(symbol => symbol.Name == readFile ? methodConversion : MethodConversion.Unknown)
						.AfterAnalyzation(AfterAnalyzation)
					)
				);
			}
		}

		[Test]
		public Task TestAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile);

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public async Task TestCancellationTokensAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile);
			var callReadFile = GetMethodName(o => o.CallReadFile);
			var callCallReadFile = GetMethodName(o => o.CallCallReadFile);
			var read = GetMethodName(() => SimpleFile.Read());
			var readAsync = GetMethodName(() => SimpleFile.ReadAsync());

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				// Check conversions
				CheckMethodsConversion(methods.Values);

				//Check token
				Assert.IsTrue(methods[readFile].CancellationTokenRequired);
				Assert.IsTrue(methods[callReadFile].CancellationTokenRequired);
				Assert.IsTrue(methods[callCallReadFile].CancellationTokenRequired);

				Assert.IsTrue(methods[readFile].OmitAsync);

				// Check MethodReferences
				Assert.AreEqual(1, methods[readFile].BodyFunctionReferences.Count());
				var methodReference = methods[readFile].BodyFunctionReferences.First();
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.PassCancellationToken);
				Assert.IsNull(methodReference.ReferenceFunction);
				Assert.AreEqual(read, methodReference.ReferenceSymbol.Name);
				Assert.AreEqual(2, methodReference.ReferenceAsyncSymbols.Count);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[0].Name);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[1].Name);

				methodReference = methods[callReadFile].BodyFunctionReferences.First();
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.PassCancellationToken);

				methodReference = methods[callCallReadFile].BodyFunctionReferences.First();
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.PassCancellationToken);
			}

			var methodConversions = new Func<IMethodSymbol, MethodConversion>[] {
				s =>  s.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown,
				s =>  s.Name == readFile ? MethodConversion.Smart : MethodConversion.Unknown,
				s =>  s.Name == callCallReadFile ? MethodConversion.ToAsync : MethodConversion.Smart
			};
			foreach (var methodConversion in methodConversions)
			{
				await ReadonlyTest(p => p
					.ConfigureAnalyzation(a => a
						.MethodConversion(methodConversion)
						.CancellationTokens(true)
						.AfterAnalyzation(AfterAnalyzation)
					)
				);
			}
		}


		[Test]
		public Task TestCancellationTokensAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile);

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(true)
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseDefaultToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokenParameterAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile);

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(ct => ct
						.ParameterGeneration(symbol => MethodCancellationToken.Required))
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseParameterToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokenOverloadAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile);

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.CancellationTokens(ct => ct
						.ParameterGeneration(symbol => MethodCancellationToken.Required | MethodCancellationToken.ForwardNone))
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseOverloadToken"), document.Transformed.ToFullString());
					})
				)
			);
		}

	}
}
