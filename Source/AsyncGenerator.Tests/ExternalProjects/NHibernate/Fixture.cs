using System;
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
					.ConfigureProject("NHibernate", p => p
						.ConfigureAnalyzation(a => a
							.MethodConversion(GetMethodConversion)
							//.UseCancellationTokenOverload(true)
							.ScanMethodBody(true)
						)
						.ConfigureTransformation(t => t
							.AsyncLock("NHibernate.Util.AsyncLock", "LockAsync")
							.LocalFunctions(true)
							.ConfigureAwaitArgument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
						)
					)
					.ApplyChanges(true)
				);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		private MethodConversion GetMethodConversion(IMethodSymbol symbol)
		{
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
				case "DefaultDynamicLazyFieldInterceptor": //The implementor of IInterceptor
				case "DefaultLazyInitializer": //The implementor of IInterceptor
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
						symbol.Name == "InitializeLazyPropertiesFromDatastore" ||
						symbol.Name == "InitializeLazyPropertiesFromCache")
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

				case "StatelessSessionImpl": // Probably bug here.
				case "SessionImpl":
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
					return MethodConversion.ToAsync;
			}
			return MethodConversion.Unknown;
		}
	}
}
