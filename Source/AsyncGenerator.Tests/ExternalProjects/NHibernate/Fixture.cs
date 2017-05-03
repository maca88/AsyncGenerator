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
					// So, we need a way to mark certain method calls as sync only.
					if (symbol.Name == "PostProcessInsert")
					{
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
				case "IPreUpdateEventListener":
					return MethodConversion.ToAsync;
			}
			return MethodConversion.Unknown;
		}
	}
}
