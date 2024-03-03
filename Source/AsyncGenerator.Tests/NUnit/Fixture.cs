using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Plugins;
using NUnit.Framework;
using AsyncGenerator.Tests.NUnit.Input;

namespace AsyncGenerator.Tests.NUnit
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestAssertThatAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThat), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AssertThat))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[Test]
		public Task TestAssertThatTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThatTryCatch), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AssertThatTryCatch))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[Test]
		public Task TestDoesNotThrowTryCatchAfterTransformation()
		{
			return ReadonlyTest(nameof(DoesNotThrowTryCatch), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(DoesNotThrowTryCatch))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[Test]
		public Task TestAssertThatNoTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertThatNoToken), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => symbol.Name == "Test" ? MethodConversion.ToAsync : MethodConversion.Smart)
					.ScanMethodBody(true)
					.CancellationTokens(o => o.RequiresCancellationToken(s => s.Name == "Test" ? false : (bool?)null))
					.PreserveReturnType(o => o.Name == "Test")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AssertThatNoToken))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[Test]
		public Task TestNestedAssertThatAfterTransformation()
		{
			return ReadonlyTest(nameof(NestedAssertThat), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
					.AlwaysAwait(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[Test]
		public Task TestAssertMultipleAfterTransformation()
		{
			return ReadonlyTest(nameof(AssertMultiple), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.PreserveReturnType(o => true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AssertMultiple))));
					})
				)
				.RegisterPlugin(new NUnitPlugin(false))
			);
		}

		[TestCase(false)]
		[TestCase(true)]
		public Task TestSimpleFixtureAfterTransformation(bool createNewTypes)
		{
			return ReadonlyTest("SimpleFixture", o => o
				.ConfigureParsing(p => p
					.AddPreprocessorSymbolName("TEST")
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(createNewTypes ? "SimpleFixtureNewType" : "SimpleFixturePartial")));
					})
				)
				.RegisterPlugin(new NUnitPlugin(createNewTypes))
			);
		}

		[Test]
		public Task TestSimpleFixtureAfterTransformationYaml()
		{
			return YamlReadonlyTest("SimpleFixture",
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: false
    assemblyName: AsyncGenerator.Core
",
				o => o
					.ConfigureParsing(p => p
						.AddPreprocessorSymbolName("TEST")
					)
					.ConfigureTransformation(t => t.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("SimpleFixturePartial")));
					}))
			);
		}

		[Test]
		public Task TestIgnoreMethodsSimpleFixtureAfterTransformationYaml()
		{
			return YamlReadonlyTest("SimpleFixture",
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Ignore
      all: true
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: false
    assemblyName: AsyncGenerator.Core
",
				o => o
					.ConfigureParsing(p => p
						.AddPreprocessorSymbolName("TEST")
					)
					.ConfigureTransformation(t => t.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					}))
			);
		}

		[Test]
		public Task TestIgnoreMethodsPostProvidersSimpleFixtureAfterTransformationYaml()
		{
			return YamlReadonlyTest("SimpleFixture",
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Ignore
      all: true
      executionPhase: PostProviders
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: false
    assemblyName: AsyncGenerator.Core
",
				o => o
					.ConfigureParsing(p => p
						.AddPreprocessorSymbolName("TEST")
					)
					.ConfigureTransformation(t => t.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("SimpleFixturePartial")));
					}))
			);
		}

		[Test]
		public Task TestIgnoreTypeSimpleFixtureAfterTransformationYaml()
		{
			return YamlReadonlyTest("SimpleFixture",
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    typeConversion:
    - conversion: Ignore
      all: true
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: false
    assemblyName: AsyncGenerator.Core
",
				o => o
					.ConfigureParsing(p => p
						.AddPreprocessorSymbolName("TEST")
					)
					.ConfigureTransformation(t => t.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(0));
					}))
			);
		}

		[Test]
		public Task TestIgnoreTypePostProvidersSimpleFixtureAfterTransformationYaml()
		{
			return YamlReadonlyTest("SimpleFixture",
				@"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    typeConversion:
    - conversion: Ignore
      all: true
      executionPhase: PostProviders
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: true
    assemblyName: AsyncGenerator.Core
",
				o => o
					.ConfigureParsing(p => p
						.AddPreprocessorSymbolName("TEST")
					)
					.ConfigureTransformation(t => t.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile("SimpleFixtureNewType")));
					}))
			);
		}
	}
}
