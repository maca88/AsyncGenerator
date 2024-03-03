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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(3));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[readFile].OmitAsync, Is.True);

				// Check InvokedBy
				Assert.That(methods[readFile].ReferencedBy.Count(), Is.EqualTo(1));
				Assert.That(methods[readFile].ReferencedBy.First(), Is.EqualTo(methods[callReadFile]));
				Assert.That(methods[callReadFile].ReferencedBy.Count(), Is.EqualTo(1));
				Assert.That(methods[callCallReadFile].ReferencedBy.Count(), Is.EqualTo(0));

				// Check MethodReferences
				Assert.That(methods[readFile].BodyFunctionReferences.Count(), Is.EqualTo(1));
				var methodReference = methods[readFile].BodyFunctionReferences.First();
				Assert.That(methodReference.ReferenceNode.Kind(), Is.EqualTo(SyntaxKind.InvocationExpression));
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.ReferenceFunction, Is.Null);
				Assert.That(methodReference.ReferenceSymbol.Name, Is.EqualTo(read));
				Assert.That(methodReference.ReferenceAsyncSymbols.Count, Is.EqualTo(1));
				Assert.That(methodReference.ReferenceAsyncSymbols[0].Name, Is.EqualTo(readAsync));

				// Check Conversion
				Assert.That(methods[readFile].Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(methods[callReadFile].Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(methods[callCallReadFile].Conversion, Is.EqualTo(MethodConversion.ToAsync));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(TestCase))));
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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(3));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				// Check conversions
				CheckMethodsConversion(methods.Values);

				//Check token
				Assert.That(methods[readFile].CancellationTokenRequired, Is.True);
				Assert.That(methods[callReadFile].CancellationTokenRequired, Is.True);
				Assert.That(methods[callCallReadFile].CancellationTokenRequired, Is.True);

				Assert.That(methods[readFile].OmitAsync, Is.True);

				// Check MethodReferences
				Assert.That(methods[readFile].BodyFunctionReferences.Count(), Is.EqualTo(1));
				var methodReference = methods[readFile].BodyFunctionReferences.First();
				Assert.That(methodReference.ReferenceNode.Kind(), Is.EqualTo(SyntaxKind.InvocationExpression));
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.PassCancellationToken, Is.True);
				Assert.That(methodReference.ReferenceFunction, Is.Null);
				Assert.That(methodReference.ReferenceSymbol.Name, Is.EqualTo(read));
				Assert.That(methodReference.ReferenceAsyncSymbols.Count, Is.EqualTo(2));
				Assert.That(methodReference.ReferenceAsyncSymbols[0].Name, Is.EqualTo(readAsync));
				Assert.That(methodReference.ReferenceAsyncSymbols[1].Name, Is.EqualTo(readAsync));

				methodReference = methods[callReadFile].BodyFunctionReferences.First();
				Assert.That(methodReference.ReferenceNode.Kind(), Is.EqualTo(SyntaxKind.InvocationExpression));
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.PassCancellationToken, Is.True);

				methodReference = methods[callCallReadFile].BodyFunctionReferences.First();
				Assert.That(methodReference.ReferenceNode.Kind(), Is.EqualTo(SyntaxKind.InvocationExpression));
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.PassCancellationToken, Is.True);
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseDefaultToken")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseParameterToken")));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseOverloadToken")));
					})
				)
			);
		}

	}
}
