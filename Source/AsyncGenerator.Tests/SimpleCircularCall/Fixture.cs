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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(3));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[readFile].ReferencedBy.Count(), Is.EqualTo(1));

				var circularMethods = new[]
				{
					method1, method2, readFile
				};
				IMethodAnalyzationResult method;
				foreach (var circularMethod in circularMethods)
				{
					method = methods[circularMethod];
					Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Input.TestCase))));
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseWithConfigureAwait")));
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
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(3));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				var method = methods[readFile];
				Assert.That(method.FunctionReferences.Count, Is.EqualTo(1));
				Assert.That(method.BodyFunctionReferences.First().PassCancellationToken, Is.True);
				Assert.That(method.CancellationTokenRequired, Is.True);

				method = methods[method2];
				Assert.That(method.FunctionReferences.Count, Is.EqualTo(2));
				foreach (var reference in method.BodyFunctionReferences)
				{
					Assert.That(reference.PassCancellationToken, Is.True);
				}
				Assert.That(method.CancellationTokenRequired, Is.True);

				method = methods[method1];
				Assert.That(method.FunctionReferences.Count, Is.EqualTo(1));
				Assert.That(method.BodyFunctionReferences.First().PassCancellationToken, Is.True);
				Assert.That(method.CancellationTokenRequired, Is.True);
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
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseWithTokens")));
					})
				)
			);
		}
	}
}
