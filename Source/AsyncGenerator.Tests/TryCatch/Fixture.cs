using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.TryCatch.Input;

namespace AsyncGenerator.Tests.TryCatch
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestElementAccessAfterTransformation()
		{
			return ReadonlyTest(nameof(ElementAccess), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ElementAccess))));
					})
				)
			);
		}

		[Test]
		public Task TestCastAfterTransformation()
		{
			return ReadonlyTest(nameof(Cast), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Cast))));
					})
				)
			);
		}

		[Test]
		public Task TestIfStatementAfterTransformation()
		{
			return ReadonlyTest(nameof(IfStatement), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(IfStatement))));
					})
				)
			);
		}

		[Test]
		public Task TestObjectCreationAfterTransformation()
		{
			return ReadonlyTest(nameof(ObjectCreation), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(ObjectCreation))));
					})
				)
			);
		}

		[Test]
		public Task TestInvocationAfterTransformation()
		{
			return ReadonlyTest(nameof(Invocation), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Invocation))));
					})
				)
			);
		}

		[Test]
		public Task TestAutoPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(AutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AutoProperty))));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractAutoPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractAutoProperty))));
					})
				)
			);
		}

		[Test]
		public Task TestAbstractAutoPropertyCatchGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(AbstractAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AbstractAutoProperty) + "CatchGetter")));
					})
				)
			);
		}

		[Test]
		public Task TestVirtualAutoPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(VirtualAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(VirtualAutoProperty))));
					})
				)
			);
		}

		[Test]
		public Task TestVirtualAutoPropertyCatchGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(VirtualAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(VirtualAutoProperty) + "CatchGetter")));
					})
				)
			);
		}

		[Test]
		public Task TestInterfaceAutoPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(InterfaceAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(InterfaceAutoProperty))));
					})
				)
			);
		}

		[Test]
		public Task TestInterfaceAutoPropertyCatchGetterAfterTransformation()
		{
			return ReadonlyTest(nameof(InterfaceAutoProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(InterfaceAutoProperty) + "CatchGetter")));
					})
				)
			);
		}

		[Test]
		public Task TestCustomPropertyAfterTransformation()
		{
			return ReadonlyTest(nameof(CustomProperty), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(CustomProperty))));
					})
				)
			);
		}

		[Test]
		public Task TestAnonymousFunctionAfterTransformation()
		{
			return ReadonlyTest(nameof(AnonymousFunction), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(AnonymousFunction))));
					})
				)
			);
		}

		[Test]
		public Task TestPreconditionAfterTransformation()
		{
			return ReadonlyTest(nameof(Precondition), p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.ToAsync)
					.ExceptionHandling(e => e
						.CatchPropertyGetterCalls(symbol => true))
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						AssertValidAnnotations(result);
						Assert.That(result.Documents.Count, Is.EqualTo(1));
						var document = result.Documents[0];
						Assert.That(document.OriginalModified, Is.Not.Null);
						Assert.That(document.Transformed.ToFullString(), Is.EqualTo(GetOutputFile(nameof(Precondition))));
					})
				)
			);
		}
	}
}
