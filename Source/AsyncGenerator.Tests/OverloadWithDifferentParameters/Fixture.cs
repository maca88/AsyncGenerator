using System;
using System.Linq;
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
		public void TestAfterAnalyzation()
		{
			var readData = GetMethodName(o => o.ReadData(null));
			var customReadData = GetMethodName(o => o.CustomReadData(null));
			var read = GetMethodName<DataReader>(o => o.Read);
			var readAsync = GetMethodName<DataReader>(o => o.ReadAsync());
			var dataReader = nameof(DataReader);
			var customDataReader = nameof(CustomDataReader);
			var overloadWithDiffParams = nameof(TestCase);

			var generator = new AsyncCodeGenerator();

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
				Assert.AreEqual(3, result.Documents[0].Namespaces[0].Types.Count);

				var types = result.Documents[0].Namespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				var type = types[overloadWithDiffParams];
				Assert.AreEqual(2, type.Methods.Count);
				var methods = type.Methods.ToLookup(o => o.Symbol.Name);

				var method = methods[readData].First();
				Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				Assert.IsTrue(method.OmitAsync);
				Assert.IsFalse(method.WrapInTryCatch);
				var methodReference = method.MethodReferences.First();
				Assert.AreEqual(ReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UseAsReturnValue);
				Assert.IsNotNull(methodReference.ReferenceFunction);

				method = methods[customReadData].First();
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);


				type = types[dataReader];
				Assert.AreEqual(3, type.Methods.Count);
				methods = type.Methods.ToLookup(o => o.Symbol.Name);

				method = methods[read].First();
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);

				method = methods[readAsync].First(o => o.Symbol.Parameters.Length == 0);
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);

				method = methods[readAsync].First(o => o.Symbol.Parameters.Length == 1);
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);


				type = types[customDataReader];
				Assert.AreEqual(2, type.Methods.Count);
				methods = type.Methods.ToLookup(o => o.Symbol.Name);

				method = methods[read].First();
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);

				method = methods[readAsync].First();
				Assert.AreEqual(MethodConversion.Ignore, method.Conversion);
			}

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
