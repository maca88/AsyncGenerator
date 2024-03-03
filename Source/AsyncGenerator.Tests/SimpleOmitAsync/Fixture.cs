using System;
using System.Linq;
using System.Threading.Tasks;
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
		public Task TestAfterAnalyzation()
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

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(13));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				IBodyFunctionReferenceAnalyzationResult methodReference;
				IMethodAnalyzationResult method;

				method = methods[simpleReturn];
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);

				method = methods[doubleCallReturn];
				Assert.That(method.OmitAsync, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(2));
				var methodReferences = method.BodyFunctionReferences.ToLookup(o => o.ReferenceSymbol.Name);
				methodReference = methodReferences[write].First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.True);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);
				methodReference = methodReferences[readFile].First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.True);
				Assert.That(methodReference.UseAsReturnValue, Is.False);
				Assert.That(methodReference.LastInvocation, Is.False);

				method = methods[syncReturn];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.True);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.Ignore));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);

				method = methods[simpleVoid];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);

				method = methods[doubleCallVoid];
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.WrapInTryCatch, Is.True);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(2));
				methodReferences = method.BodyFunctionReferences.ToLookup(o => o.ReferenceSymbol.Name);
				methodReference = methodReferences[read].First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);
				methodReference = methodReferences[syncReadFile].First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.Ignore));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.False);
				Assert.That(methodReference.LastInvocation, Is.False);

				method = methods[expressionVoid];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);

				method = methods[expressionReturn];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(1));
				methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.LastInvocation, Is.True);

				method = methods[returnString];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(0));

				method = methods[returnDefaultString];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(0));

				method = methods[returnDecimal];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(0));

				method = methods[returnDecimalConstructor];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				Assert.That(method.SplitTail, Is.False);
				Assert.That(method.BodyFunctionReferences.Count(), Is.EqualTo(0));
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
						symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString ||
						symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
							? MethodConversion.ToAsync
							: MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
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
		public Task TestConfigureAwaitAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.ConfigureAwaitArgument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
					.AfterTransformation(result =>
					{
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("TestCaseConfigureAwait")));
					})
				)
			);
		}

		[Test]
		public Task TestUseCancellationTokenOverloadAfterTransformation()
		{
			var syncReturn = GetMethodName(o => o.SyncReturn());
			var returnString = GetMethodName(o => o.SimpleReturnString());
			var returnDefaultString = GetMethodName(o => o.SimpleReturnDefaultOfString());
			var returnDecimal = GetMethodName(o => o.SimpleReturnDecimal());
			var returnDecimalConstructor = GetMethodName(o => o.ReturnDecimalConstructor());

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == syncReturn || symbol.Name == returnString || symbol.Name == returnDefaultString || symbol.Name == returnDecimal || symbol.Name == returnDecimalConstructor
						? MethodConversion.ToAsync
						: MethodConversion.Smart)
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
