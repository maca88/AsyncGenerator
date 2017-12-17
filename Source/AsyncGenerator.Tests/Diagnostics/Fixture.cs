using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Diagnostics.Input;

namespace AsyncGenerator.Tests.Diagnostics
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			bool val;
			var test = GetMethodName(o => o.Test(out val));
			var test2 = GetMethodName(o => o.Test2(out val));
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(result =>
					{
						var methods = result.Documents.First().AllTypes.First().Methods.ToDictionary(o => o.Symbol.Name);
						var testMethod = (MethodData) methods[test];
						Assert.AreEqual(DiagnosticSeverity.Warning, testMethod.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(1, testMethod.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, testMethod.GetDiagnostics().First().DiagnosticSeverity);

						var test2Method = (MethodData)methods[test2];
						Assert.AreEqual(DiagnosticSeverity.Warning, test2Method.IgnoredReason.DiagnosticSeverity);
						Assert.AreEqual(1, test2Method.GetDiagnostics().Count());
						Assert.AreEqual(DiagnosticSeverity.Warning, test2Method.GetDiagnostics().First().DiagnosticSeverity);
					})
				)
				.ConfigureTransformation(t => t.Disable())
			);
		}
	}
}
