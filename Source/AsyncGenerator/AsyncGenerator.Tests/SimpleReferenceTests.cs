using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.TestCases;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class SimpleReferenceTests : BaseTest<SimpleReference>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile);
			var callReadFile = GetMethodName(o => o.CallReadFile);
			var callCallReadFile = GetMethodName(o => o.CallCallReadFile);
			var read = GetMethodName(() => SimpleFile.Read());
			var readAsync = GetMethodName(() => SimpleFile.ReadAsync());

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.IsTrue(methods[readFile].OmitAsync);

				// Check InvokedBy
				Assert.AreEqual(1, methods[readFile].InvokedBy.Count);
				Assert.AreEqual(methods[callReadFile], methods[readFile].InvokedBy[0]);
				Assert.AreEqual(1, methods[callReadFile].InvokedBy.Count);
				Assert.AreEqual(0, methods[callCallReadFile].InvokedBy.Count);

				// Check MethodReferences
				Assert.AreEqual(1, methods[readFile].MethodReferences.Count);
				var methodReference = methods[readFile].MethodReferences[0];
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.IsFalse(methodReference.Ignore);
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsNull(methodReference.ReferenceFunctionData);
				Assert.AreEqual(read, methodReference.ReferenceSymbol.Name);
				Assert.AreEqual(1, methodReference.ReferenceAsyncSymbols.Count);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[0].Name);

				// Check Conversion
				Assert.AreEqual(MethodConversion.ToAsync, methods[readFile].Conversion);
				Assert.AreEqual(MethodConversion.ToAsync, methods[callReadFile].Conversion);
				Assert.AreEqual(MethodConversion.ToAsync, methods[callCallReadFile].Conversion);

			};

			var methodConversions = new[] {MethodConversion.ToAsync, MethodConversion.Smart};
			foreach (var methodConversion in methodConversions)
			{
				var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return symbol.Name == readFile ? methodConversion : MethodConversion.Unknown;
					})
					.Callbacks(c => c.AfterAnalyzation(afterAnalyzationFn))
				)
				);
				Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
			}
		}

		[Test]
		public void TestUseCancellationTokenOverloadAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile);
			var callReadFile = GetMethodName(o => o.CallReadFile);
			var callCallReadFile = GetMethodName(o => o.CallCallReadFile);
			var read = GetMethodName(() => SimpleFile.Read());
			var readAsync = GetMethodName(() => SimpleFile.ReadAsync());

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				// Check conversions
				CheckMethodsConversion(methods.Values);

				//Check token
				Assert.IsTrue(methods[readFile].CancellationTokenRequired);
				Assert.IsTrue(methods[callReadFile].CancellationTokenRequired);
				Assert.IsTrue(methods[callCallReadFile].CancellationTokenRequired);

				Assert.IsTrue(methods[readFile].OmitAsync);

				// Check MethodReferences
				Assert.AreEqual(1, methods[readFile].MethodReferences.Count);
				var methodReference = methods[readFile].MethodReferences[0];
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.IsFalse(methodReference.Ignore);
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.CancellationTokenRequired);
				Assert.IsNull(methodReference.ReferenceFunctionData);
				Assert.AreEqual(read, methodReference.ReferenceSymbol.Name);
				Assert.AreEqual(2, methodReference.ReferenceAsyncSymbols.Count);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[0].Name);
				Assert.AreEqual(readAsync, methodReference.ReferenceAsyncSymbols[1].Name);

				methodReference = methods[callReadFile].MethodReferences[0];
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.IsFalse(methodReference.Ignore);
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.CancellationTokenRequired);

				methodReference = methods[callCallReadFile].MethodReferences[0];
				Assert.AreEqual(SyntaxKind.InvocationExpression, methodReference.ReferenceNode.Kind());
				Assert.IsFalse(methodReference.Ignore);
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.CancellationTokenRequired);
			};

			var methodConversions = new Func<IMethodSymbol, MethodConversion>[] {
				s =>  s.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown,
				s =>  s.Name == readFile ? MethodConversion.Smart : MethodConversion.Unknown,
				s =>  s.Name == callCallReadFile ? MethodConversion.ToAsync : MethodConversion.Smart
			};
			foreach (var methodConversion in methodConversions)
			{
				var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(methodConversion)
					.UseCancellationTokenOverload(true)
					.Callbacks(c => c.AfterAnalyzation(afterAnalyzationFn))
				)
				);
				Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
				Console.WriteLine("Passed");
			}
		}


	}
}
