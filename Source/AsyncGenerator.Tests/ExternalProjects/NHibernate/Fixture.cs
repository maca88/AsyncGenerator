﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
	/// <summary>
	/// Transformation for the NHibernate project. 
	/// Before running the test the following steps needs to be done:
	///		- Fetch the NHibernate submodule
	///		- Run the script to generate the SharedAssembly.cs
	///		- Restore nuget packages for the NHibernate solution
	///		- Run the test
	/// </summary>
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Explicit]
		[Test]
		public void TestAfterTransformation()
		{
			var slnFilePath = Path.GetFullPath(GetBaseDirectory() + @"..\..\ExternalProjects\NHibernate\Source\src\NHibernate.sln");
			var config = AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, s => s
					.RunInParallel()
					.ConfigureProject("NHibernate", p => p
						.ConfigureAnalyzation(a => a
							.MethodConversion(GetMethodConversion)
							.CallForwarding(true)
							.CancellationTokens(t => t
								.Guards(true)
								.MethodGeneration(symbolInfo =>
								{
									if (IsPubliclyExposedType(symbolInfo.Symbol.ContainingType) || // For public types generate default parameter
										symbolInfo.ImplementedInterfaces.Any(o => IsPubliclyExposedType(o.ContainingType))) // The rule for public types shall be passed to implementors
									{
										return MethodCancellationToken.DefaultParameter;
									}
									return MethodCancellationToken.Parameter;
								})
								.RequiresCancellationToken(symbol =>
								{
									if (IsEventListener(symbol.ContainingType))
									{
										return true;
									}
									return null; // Leave the decision to the generator
								})
							)
							.ScanMethodBody(true)
						)
						.ConfigureTransformation(t => t
							.AsyncLock("NHibernate.Util.AsyncLock", "LockAsync")
							.LocalFunctions(true)
							.ConfigureAwaitArgument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
						)
						.RegisterPlugin<TransactionScopeRewriter>() // Rewrite TransactionScope in AdoNetWithDistributedTransactionFactory
					)
					.ConfigureProject("NHibernate.DomainModel", p => p
						.ConfigureAnalyzation(a => a
							.ScanForMissingAsyncMembers(true)
							.ScanMethodBody(true)
						)
					)
					.ConfigureProject("NHibernate.Test", p => p
						.ConfigureAnalyzation(a => a
							.MethodConversion(symbol =>
								{
									if (symbol.GetAttributes().Any(o => o.AttributeClass.Name == "IgnoreAttribute"))
									{
										return MethodConversion.Ignore;
									}
									if (symbol.GetAttributes().Any(o => o.AttributeClass.Name == "TestAttribute"))
									{
										return MethodConversion.Smart;
									}
									return MethodConversion.Unknown;
								})
							.PreserveReturnType(symbol => symbol.GetAttributes().Any(o => o.AttributeClass.Name == "TestAttribute"))
							.ScanForMissingAsyncMembers(o => o.AllInterfaces.Any(i => i.ContainingAssembly.Name == "NHibernate"))
							.CancellationTokens(t => t
								.RequiresCancellationToken(symbol => symbol.GetAttributes().Any(o => o.AttributeClass.Name == "TestAttribute") ? (bool?)false : null))
							.ScanMethodBody(true)
							.DocumentSelection(doc =>
							{
								return
									// AsQueryable method is called on a retrieved list from db and the result is used elsewhere in code
									!doc.FilePath.EndsWith(@"Linq\MathTests.cs")
									// It looks like that GC.Collect works differently with async.
									// if "await Task.Yield();" is added after DoLinqInSeparateSessionAsync then the test runs successfully (TODO: discover why)
									&& !doc.FilePath.EndsWith(@"Linq\ExpressionSessionLeakTest.cs")
									;
							})
							.TypeConversion(type =>
								{
									if (type.Name == "NorthwindDbCreator" || // Ignored for performance reasons
										type.Name == "ObjectAssert" ||  // Has a TestFixture attribute but is not a test
										type.Name == "LinqReadonlyTestsContext") // SetUpFixture
									{
										return TypeConversion.Ignore;
									}
									if (type.GetAttributes().Any(o => o.AttributeClass.Name == "IgnoreAttribute"))
									{
										return TypeConversion.Ignore;
									}
									if (type.GetAttributes().Any(o => o.AttributeClass.Name == "TestFixtureAttribute"))
									{
										return TypeConversion.NewType;
									}
									var currentType = type;
									while (currentType != null)
									{
										if (currentType.Name == "TestCase")
										{
											return TypeConversion.Ignore;
										}
										currentType = currentType.BaseType;
									}
									return TypeConversion.Unknown;
								})
						)
						.RegisterPlugin<TransactionScopeRewriter>()
						.RegisterPlugin<LinqAsyncCounterpartsFinder>()
						.RegisterPlugin<NUnitAsyncCountepartFinder>()
					)
					.ApplyChanges(true)
				);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		static bool IsPubliclyExposedType(ISymbol type)
		{
			var ns = type.ContainingNamespace?.ToString();
			if (ns == "NHibernate")
				return true;

			var typeName = type.ToString();
			return
				typeName == "NHibernate.Tool.hbm2ddl.SchemaUpdate" ||
				typeName == "NHibernate.Tool.hbm2ddl.SchemaValidator" ||
				typeName == "NHibernate.Tool.hbm2ddl.SchemaExport";
		}

		private MethodConversion GetMethodConversion(IMethodSymbol symbol)
		{
			if (symbol.GetAttributes().Any(a => a.AttributeClass.Name == "ObsoleteAttribute"))
			{
				return MethodConversion.Ignore;
			}
			switch (symbol.ContainingType.Name)
			{
				case "HqlSqlWalker":
					// The Seed method that was recognized to be async is never used as async.
					// IsIntegral checks that the type is either short, int, or long; 
					// the only type where SeedAsync is possible is DbTimestampType type and it fails into the "else if" branch. 
					if (symbol.Name == "PostProcessInsert")
					{
						return MethodConversion.Ignore;
					}
					break;
				case "SqlStatementLogger": //Generated because of Console.WriteLineAsync
					return MethodConversion.Ignore;
				case "IInterceptor": //We need to adjust proxy code generation to call IInterceptor async
				case "IFieldInterceptor": //Is called by DefaultDynamicLazyFieldInterceptor.Intercept
				case "AbstractFieldInterceptor":// The implementor of IFieldInterceptor
					if (symbol.Name == "Intercept" || 
						symbol.Name == "InitializeOrGetAssociation"//Private method of AbstractFieldInterceptor
						) 
					{
						return MethodConversion.Ignore;
					}
					break;
				case "ILazyPropertyInitializer": // Is called by AbstractFieldInterceptor.Intercept
				case "AbstractEntityPersister":
					if (symbol.Name == "InitializeLazyProperty" || 
						symbol.Name == "InitializeLazyPropertiesFromDatastore" || // Private method from AbstractEntityPersister
						symbol.Name == "InitializeLazyPropertiesFromCache") // Private method from AbstractEntityPersister
					{
						return MethodConversion.Ignore;
					}
					break;
				case "BasicLazyInitializer": // Is called by DefaultLazyInitializer.Intercept
					if (symbol.Name == "Invoke")
					{
						return MethodConversion.Ignore;
					}
					break;
				case "SqlGenerator":
				case "HqlLexer":
					if (symbol.Name == "EvaluatePredicate")
					{
						// Generated because of Console.WriteLineAsync
						return MethodConversion.Ignore;
					}
					break;
				case "StatefulPersistenceContext":
					if (
						symbol.Name == "SetReadOnly" || // Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						symbol.Name == "Unproxy" // Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						)
					{
					
						return MethodConversion.Ignore;
					}
					break;

				case "CollectionType":
					if (symbol.Name == "Contains")
					{
						// Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						return MethodConversion.Ignore;
					}
					break;

				case "NHibernateProxyHelper":
					if (symbol.Name == "GuessClass")
					{
						// Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						return MethodConversion.Ignore;
					}
					break;

				case "NHibernateUtil":
					if (symbol.Name == "IsPropertyInitialized")
					{
						// Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						return MethodConversion.Ignore;
					}
					break;
				case "ISession":
				case "ISessionImplementor":
					if (symbol.Name == "BestGuessEntityName" || 
						symbol.Name == "Contains")
					{
						// Generated because of proxy.HibernateLazyInitializer.GetImplementation, but it's not async in this case
						return MethodConversion.Ignore;
					}
					break;

				case "UnsavedValueFactory":
					if (symbol.Name == "GetUnsavedVersionValue")
					{
						//The Seed method that was recognized to be async is never used as async.
						return MethodConversion.Ignore;
					}
					break;

				case "AbstractPersistentCollection":
					if (symbol.Name == "ReadSize" ||
					    symbol.Name == "ReadIndexExistence" ||
					    symbol.Name == "ReadElementExistence" ||
					    symbol.Name == "ReadElementByIndex" ||
					    symbol.Name == "Read" || // Called by methods above
					    symbol.Name == "Write"
					)
					{
						//No one calls us :'(
						return MethodConversion.Ignore;
					}
					break;

				case "ICollectionPersister": // Called by AbstractPersistentCollection
				case "AbstractCollectionPersister": // Implementation of ICollectionPersister
					if (symbol.Name == "GetSize" ||
					    symbol.Name == "IndexExists" ||
					    symbol.Name == "ElementExists" ||
					    symbol.Name == "GetElementByIndex" || 
						symbol.Name == "Exists"
					)
					{
						//No one calls us :'(
						return MethodConversion.Ignore;
					}
					break;
				case "IBatcher":
					if (symbol.Name == "ExecuteReader" || symbol.Name == "ExecuteNonQuery")
					{
						return MethodConversion.ToAsync;
					}
					break;
				case "NullableType":
					if (symbol.Name == "NullSafeGet")
					{
						return MethodConversion.Ignore;
					}
					break;
				default:
					if (IsEventListener(symbol.ContainingType))
					{
						return MethodConversion.ToAsync;
					}
					break;
			}
			return MethodConversion.Unknown;
		}

		private bool IsEventListener(INamedTypeSymbol type)
		{
			switch (type.Name)
			{
				case "IAutoFlushEventListener":
				case "IFlushEventListener":
				case "IDeleteEventListener":
				case "ISaveOrUpdateEventListener":
				case "IPostCollectionRecreateEventListener":
				case "IPostCollectionRemoveEventListener":
				case "IPostCollectionUpdateEventListener":
				case "IPostDeleteEventListener":
				case "IPostInsertEventListener":
				case "IPostUpdateEventListener":
				case "IPreCollectionRecreateEventListener":
				case "IPreCollectionRemoveEventListener":
				case "IPreCollectionUpdateEventListener":
				case "IPreDeleteEventListener":
				case "IPreInsertEventListener":
				case "IPreLoadEventListener":
				case "IPreUpdateEventListener":
					return true;
			}
			return false;
		}
	}
}
