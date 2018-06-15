using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.SearchInheritedTypes.Input;

namespace AsyncGenerator.Tests.SearchInheritedTypes
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		private const string YamlConfiguration = @"projects:
- filePath: AsyncGenerator.Tests.csproj
  analyzation:
    methodConversion:
    - conversion: Smart
      all: true
    searchAsyncCounterpartsInInheritedTypes: true
";

		private const string XmlConfiguration = @"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Projects>
    <Project filePath=""AsyncGenerator.Tests.csproj"">
      <Analyzation>
        <MethodConversion>
          <Method conversion=""Smart"" all=""true"" />
        </MethodConversion>
        <SearchAsyncCounterpartsInInheritedTypes>true</SearchAsyncCounterpartsInInheritedTypes>
      </Analyzation>
    </Project>
  </Projects>
</AsyncGenerator>
";

		[Test]
		public Task TestInterfaceAfterTransformation()
		{
			return ReadonlyTest(nameof(Interface), p => p
				.ConfigureAnalyzation(a => a
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterInterafaceTransformantion)
				)
			);
		}

		[Test]
		public Task TestYamlInterfaceAfterTransformation()
		{
			return YamlReadonlyTest(nameof(Interface), YamlConfiguration, p => p
					.ConfigureTransformation(t => t.AfterTransformation(AfterInterafaceTransformantion))
			);
		}

		[Test]
		public Task TestXmlInterfaceAfterTransformation()
		{
			return XmlReadonlyTest(nameof(Interface), XmlConfiguration, p => p
					.ConfigureTransformation(t => t.AfterTransformation(AfterInterafaceTransformantion))
			);
		}

		private void AfterInterafaceTransformantion(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Assert.AreEqual(GetOutputFile(nameof(Interface)), document.Transformed.ToFullString());
		}


		[Test]
		public Task TestAbstractClassAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractClass), p => p
				.ConfigureAnalyzation(a => a
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(AfterAbstractClassTransformantion)
				)
			);
		}

		[Test]
		public Task TestYamlAbstractClassAfterTransformation()
		{
			return YamlReadonlyTest(nameof(AbstractClass), YamlConfiguration, p => p
				.ConfigureTransformation(t => t.AfterTransformation(AfterAbstractClassTransformantion))
			);
		}

		[Test]
		public Task TestXmlAbstractClassAfterTransformation()
		{
			return XmlReadonlyTest(nameof(AbstractClass), XmlConfiguration, p => p
				.ConfigureTransformation(t => t.AfterTransformation(AfterAbstractClassTransformantion))
			);
		}

		private void AfterAbstractClassTransformantion(IProjectTransformationResult result)
		{
			AssertValidAnnotations(result);
			Assert.AreEqual(1, result.Documents.Count);
			var document = result.Documents[0];
			Assert.NotNull(document.OriginalModified);
			Assert.AreEqual(GetOutputFile(nameof(AbstractClass)), document.Transformed.ToFullString());
		}


		[Test]
		public Task TestMultipleAsyncCounterpartsAfterTransformation()
		{
			return ReadonlyTest(nameof(MultipleAsyncCounterparts), p => p
				.ConfigureAnalyzation(a => a
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MultipleAsyncCounterparts)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMultipleAsyncCounterpartsWithTokenAfterTransformation()
		{
			return ReadonlyTest(nameof(MultipleAsyncCounterparts), p => p
				.ConfigureAnalyzation(a => a
					.SearchAsyncCounterpartsInInheritedTypes(true)
					.CancellationTokens(true)
					.ScanForMissingAsyncMembers(true)
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MultipleAsyncCounterparts) + "Token"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestMissingAsyncMembersAfterTransformation()
		{
			return ReadonlyTest(nameof(MissingAsyncMembers), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ScanForMissingAsyncMembers(true)
					.SearchAsyncCounterpartsInInheritedTypes(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.IsNotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(MissingAsyncMembers)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestGenericClassAfterTransformation()
		{
			return ReadonlyTest(nameof(GenericClass), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.ScanForMissingAsyncMembers(true)
					.SearchAsyncCounterpartsInInheritedTypes(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.AreEqual(0, result.Documents.Count);
					})
				)
			);
		}
	}
}
