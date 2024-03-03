using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Tests.OverloadWithDifferentParameters.Input;
using NUnit.Framework;

namespace AsyncGenerator.Tests.OverloadWithDifferentParameters
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var readData = GetMethodName(o => o.ReadData(null));
			var customReadData = GetMethodName(o => o.CustomReadData(null));
			var read = GetMethodName<DataReader>(o => o.Read);
			var readAsync = GetMethodName<DataReader>(o => o.ReadAsync());
			var dataReader = nameof(DataReader);
			var customDataReader = nameof(CustomDataReader);
			var overloadWithDiffParams = nameof(TestCase);

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.That(result.Documents.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces.Count, Is.EqualTo(1));
				Assert.That(result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count, Is.EqualTo(3));

				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				var type = types[overloadWithDiffParams];
				Assert.That(type.Methods.Count, Is.EqualTo(2));
				var methods = type.Methods.ToLookup(o => o.Symbol.Name);

				var method = methods[readData].First();
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.ToAsync));
				Assert.That(method.OmitAsync, Is.True);
				Assert.That(method.WrapInTryCatch, Is.False);
				var methodReference = method.BodyFunctionReferences.First();
				Assert.That(methodReference.GetConversion(), Is.EqualTo(ReferenceConversion.ToAsync));
				Assert.That(methodReference.AwaitInvocation, Is.False);
				Assert.That(methodReference.UseAsReturnValue, Is.True);
				Assert.That(methodReference.ReferenceFunction, Is.Not.Null);

				method = methods[customReadData].First();
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));


				type = types[dataReader];
				Assert.That(type.Methods.Count, Is.EqualTo(3));
				methods = type.Methods.ToLookup(o => o.Symbol.Name);

				method = methods[read].First();
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));

				method = methods[readAsync].First(o => o.Symbol.Parameters.Length == 0);
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));

				method = methods[readAsync].First(o => o.Symbol.Parameters.Length == 1);
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));


				type = types[customDataReader];
				Assert.That(type.Methods.Count, Is.EqualTo(2));
				methods = type.Methods.ToLookup(o => o.Symbol.Name);

				method = methods[read].First();
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));

				method = methods[readAsync].First();
				Assert.That(method.Conversion, Is.EqualTo(MethodConversion.Ignore));
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}
	}
}
