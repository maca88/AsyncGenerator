using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.SimpleOmitAsync.Input;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleOmitAsync
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
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
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(13, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				IBodyFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;

				method = methods[simpleReturn];
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.AreEqual(1, method.MethodReferences.Count);
				methodReference = method.MethodReferences[0];
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				methodReference = methodReferences[readFile].First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsTrue(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);
				methodReference = methodReferences[syncReadFile].First();
				Assert.AreEqual(ReferenceConversion.Ignore, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsFalse(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
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
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsTrue(methodReference.LastInvocation);

				method = methods[returnString];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.MethodReferences.Count);

				method = methods[returnDefaultString];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.MethodReferences.Count);

				method = methods[returnDecimal];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.MethodReferences.Count);

				method = methods[returnDecimalConstructor];
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsTrue(method.WrapInTryCatch);
				Assert.IsFalse(method.SplitTail);
				Assert.AreEqual(0, method.MethodReferences.Count);
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
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
		public void TestConfigureAwaitAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.ConfigureAwaitArgument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseConfigureAwait"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestUseCancellationTokenOverloadAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
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
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
