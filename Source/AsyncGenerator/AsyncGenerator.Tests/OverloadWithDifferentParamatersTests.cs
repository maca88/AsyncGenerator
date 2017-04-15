using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.TestCases;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class OverloadWithDifferentParamatersTests : BaseTest<OverloadWithDifferentParamaters>
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
			var overloadWithDiffParams = nameof(OverloadWithDifferentParamaters);

			var generator = new AsyncCodeGenerator();
			Action<IProjectAnalyzationResult> afterAnalyzationFn = result =>
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
				Assert.AreEqual(FunctionReferenceConversion.ToAsync, methodReference.GetConversion());
				Assert.IsFalse(methodReference.Ignore);
				Assert.IsFalse(methodReference.AwaitInvocation);
				Assert.IsTrue(methodReference.UsedAsReturnValue);
				Assert.IsNotNull(methodReference.ReferenceFunctionData);

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
			};

			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
					.Callbacks(c => c.AfterAnalyzation(afterAnalyzationFn))
				)
				);
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
