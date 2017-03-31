using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Tests.Partial.SimpleClass;
using AsyncGenerator.Tests.Partial.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Partial
{
	public class PartialTest : BaseTest
	{
		public PartialTest() : base("Partial/TestCases")
		{
		}

		[Test]
		public void TestSimpleReferenceAfterAnalyzation()
		{
			var readFile = GetMethodName<SimpleReference>(o => o.ReadFile);
			var callReadFile = GetMethodName<SimpleReference>(o => o.CallReadFile);
			var callCallReadFile = GetMethodName<SimpleReference>(o => o.CallCallReadFile);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].InvokedBy.Count);
				Assert.AreEqual(methods[callReadFile], methods[readFile].InvokedBy[0]);

				Assert.AreEqual(1, methods[callReadFile].InvokedBy.Count);
				Assert.AreEqual(methods[callCallReadFile], methods[callReadFile].InvokedBy[0]);
			};
			var config = Configure(nameof(SimpleReference), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversionFunction(symbol =>
					{
						return symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown;
					})
					.Callbacks(c => c
						.AfterAnalyzation(afterAnalyzationFn)
					)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestSimpleAnonymousFunctionsAfterAnalyzation()
		{
			var readFile = GetMethodName<SimpleAnonymousFunctions>(o => o.ReadFile(null));
			var declareAction = GetMethodName<SimpleAnonymousFunctions>(o => o.DeclareAction());
			var declareFunction = GetMethodName<SimpleAnonymousFunctions>(o => o.DeclareFunction());
			var declareNamedDelegate = GetMethodName<SimpleAnonymousFunctions>(o => o.DeclareNamedDelegate());
			var returnDelegate = GetMethodName<SimpleAnonymousFunctions>(o => o.ReturnDelegate());
			var argumentAction = GetMethodName<SimpleAnonymousFunctions>(o => o.ArgumentAction);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
				Assert.AreEqual(6, result.Documents[0].Namespaces[0].Types[0].Methods.Count);
				var methods = result.Documents[0].Namespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(5, methods[readFile].InvokedBy.Count);

				var readFileMethod = methods[readFile];

				var ignoredAnonymousMethods = new[]
				{
					declareNamedDelegate,
					returnDelegate,
					declareFunction,
					declareAction
				};
				IMethodAnalyzationResult method;
				foreach (var ignoredAnonymousMethod in ignoredAnonymousMethods)
				{
					method = methods[ignoredAnonymousMethod];
					Assert.AreEqual(MethodConversion.Unknown, method.Conversion);
					Assert.AreEqual(1, method.AnonymousFunctions.Count);
					Assert.AreEqual(MethodConversion.Ignore, method.AnonymousFunctions[0].Conversion);
					Assert.AreEqual(1, method.AnonymousFunctions[0].MethodReferences.Count);
					Assert.IsTrue(readFileMethod.InvokedBy.Any(o => o == method.AnonymousFunctions[0]));
				}

				method = methods[argumentAction];
				Assert.AreEqual(MethodConversion.Unknown, method.Conversion);
				Assert.AreEqual(1, method.AnonymousFunctions.Count);
				Assert.AreEqual(MethodConversion.Unknown, method.AnonymousFunctions[0].Conversion);
				Assert.AreEqual(1, method.AnonymousFunctions[0].MethodReferences.Count);
				Assert.IsTrue(readFileMethod.InvokedBy.Any(o => o == method.AnonymousFunctions[0]));
			};
			var config = Configure(nameof(SimpleAnonymousFunctions), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversionFunction(symbol =>
					{
						return symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Unknown;
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
