using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Tests.TestCases;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class PreconditionsTests : BaseTest<Preconditions>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types.Count);

				var types = result.Documents[0].Namespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(3, types[nameof(Preconditions)] .Methods.Count);
				var methods = types[nameof(Preconditions)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].Preconditions.Count);
				Assert.AreEqual(0, methods[divideShort].Preconditions.Count);
				Assert.AreEqual(1, methods[divide].Preconditions.Count);

				foreach (var method in methods.Values)
				{
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
					.Callbacks(c => c
						.AfterAnalyzation(afterAnalyzationFn)
					)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestCustomPreconditionCheckerAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types.Count);

				var types = result.Documents[0].Namespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(3, types[nameof(Preconditions)].Methods.Count);
				var methods = types[nameof(Preconditions)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].Preconditions.Count);
				Assert.AreEqual(1, methods[divideShort].Preconditions.Count);
				Assert.AreEqual(1, methods[divide].Preconditions.Count);

				foreach (var method in methods.Values)
				{
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			};
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
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
					.Callbacks(c => c
						.AfterAnalyzation(afterAnalyzationFn)
					)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
