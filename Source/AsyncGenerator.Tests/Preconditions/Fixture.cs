using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Preconditions.Input;

namespace AsyncGenerator.Tests.Preconditions
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(2));

				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.That(types[nameof(TestCase)].Methods.Count, Is.EqualTo(3));
				var methods = types[nameof(TestCase)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[readFile].Preconditions.Count, Is.EqualTo(1));
				Assert.That(methods[readFile].SplitTail, Is.False);

				Assert.That(methods[divideShort].Preconditions.Count, Is.EqualTo(0));
				Assert.That(methods[divideShort].SplitTail, Is.False);

				Assert.That(methods[divide].Preconditions.Count, Is.EqualTo(1));
				Assert.That(methods[divide].SplitTail, Is.True);

				foreach (var method in methods.Values)
				{
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
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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
		public Task TestLocalFunctionAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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

		[Test]
		public Task TestCustomPreconditionCheckerAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(2));

				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.That(types[nameof(Input.TestCase)].Methods.Count, Is.EqualTo(3));
				var methods = types[nameof(Input.TestCase)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[readFile].Preconditions.Count, Is.EqualTo(1));
				Assert.That(methods[readFile].SplitTail, Is.False);

				Assert.That(methods[divideShort].Preconditions.Count, Is.EqualTo(1));
				Assert.That(methods[divideShort].SplitTail, Is.True);

				Assert.That(methods[divide].Preconditions.Count, Is.EqualTo(1));
				Assert.That(methods[divide].SplitTail, Is.True);

				foreach (var method in methods.Values)
				{
					Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.IsPrecondition((statement, semanticModel) =>
					{
						var expressionStatement = statement as ExpressionStatementSyntax;
						var invocationExpression = expressionStatement?.Expression as InvocationExpressionSyntax;
						if (invocationExpression == null)
						{
							return false;
						}
						var symbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol;
						return symbol?.ContainingType?.Name == "Requires";
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestCustomPreconditionCheckerAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.IsPrecondition((statement, semanticModel) =>
					{
						var expressionStatement = statement as ExpressionStatementSyntax;
						var invocationExpression = expressionStatement?.Expression as InvocationExpressionSyntax;
						if (invocationExpression == null)
						{
							return false;
						}
						var symbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol;
						return symbol?.ContainingType?.Name == "Requires";
					})
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseCustomChecker")));
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
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

		[Test]
		public Task TestCancellationTokensLocalFunctionsAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseLocalFunctionsWithTokens")));
					})
				)
			);
		}
	}
}
