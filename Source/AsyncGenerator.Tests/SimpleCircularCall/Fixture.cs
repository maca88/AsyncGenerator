using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleCircularCall
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile());
			var method1 = GetMethodName(o => o.Method1);
			var method2 = GetMethodName(o => o.Method2);

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].ReferencedBy.Count());

				var circularMethods = new[]
				{
					method1, method2, readFile
				};
				IMethodAnalyzationResult method;
				foreach (var circularMethod in circularMethods)
				{
					method = methods[circularMethod];
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(Input.TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestConfigureAwaitAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
				)
				.ConfigureTransformation(t => t
					.ConfigureAwaitArgument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseWithConfigureAwait"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestUseCancellationTokenOverloadAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile());
			var method1 = GetMethodName(o => o.Method1);
			var method2 = GetMethodName(o => o.Method2);

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				var method = methods[readFile];
				Assert.AreEqual(1, method.FunctionReferences.Count);
				Assert.IsTrue(method.BodyFunctionReferences.First().PassCancellationToken);
				Assert.IsTrue(method.CancellationTokenRequired);

				method = methods[method2];
				Assert.AreEqual(2, method.FunctionReferences.Count);
				foreach (var reference in method.BodyFunctionReferences)
				{
					Assert.IsTrue(reference.PassCancellationToken);
				}
				Assert.IsTrue(method.CancellationTokenRequired);

				method = methods[method1];
				Assert.AreEqual(1, method.FunctionReferences.Count);
				Assert.IsTrue(method.BodyFunctionReferences.First().PassCancellationToken);
				Assert.IsTrue(method.CancellationTokenRequired);
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
					.CancellationTokens(true)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestUseCancellationTokenOverloadAfterTransformation()
		{
			var readFile = GetMethodName(o => o.ReadFile());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
