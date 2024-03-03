using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using NUnit.Framework;

namespace AsyncGenerator.Tests.SimpleAnonymousFunctions
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var readFile = GetMethodName(o => o.ReadFile());
			var declareAction = GetMethodName(o => o.DeclareAction());
			var declareFunction = GetMethodName(o => o.DeclareFunction());
			var declareNamedDelegate = GetMethodName(o => o.DeclareNamedDelegate());
			var returnDelegate = GetMethodName(o => o.ReturnDelegate());
			var argumentAction = GetMethodName(o => o.ArgumentAction);

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Conversion, Is.EqualTo(TypeConversion.Partial));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.Count, Is.EqualTo(6));
				var methods = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types[0].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.That(methods[readFile].ReferencedBy.Count(), Is.EqualTo(5));

				var readFileMethod = methods[readFile];

				var ignoredAnonymousMethods = new[]
				{
					declareNamedDelegate, returnDelegate, declareFunction, declareAction
				};
				IMethodAnalyzationResult method;
				foreach (var ignoredAnonymousMethod in ignoredAnonymousMethods)
				{
					method = methods[ignoredAnonymousMethod];
					Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));
					Assert.That(method.ChildFunctions.Count, Is.EqualTo(1));
					Assert.That(method.ChildFunctions[0].Conversion, Is.EqualTo(MethodConversion.Ignore));
					Assert.That(method.ChildFunctions[0].FunctionReferences.Count, Is.EqualTo(1));
					Assert.That(readFileMethod.ReferencedBy.Any(o => o == method.ChildFunctions[0]), Is.True);
				}

				method = methods[argumentAction];
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.ChildFunctions.Count, Is.EqualTo(1));
				Assert.That(method.ChildFunctions[0].Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.ChildFunctions[0].FunctionReferences.Count, Is.EqualTo(1));
				Assert.That(readFileMethod.ReferencedBy.Any(o => o == method.ChildFunctions[0]), Is.True);
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == readFile ? MethodConversion.ToAsync : MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}
	}
}
