using System;
using System.IO;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
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
							.ScanMethodBody(true)
						)
						.ConfigureTransformation(t => t
							.AsyncLock("NHibernate.Util.AsyncLock", "LockAsync")
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
				case "IBatcher":
					if (symbol.Name == "ExecuteReader" || symbol.Name == "ExecuteNonQuery")
					{
						return MethodConversion.ToAsync;
					}
					break;/*
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
					return MethodConversion.ToAsync;*/
			}
			return MethodConversion.Unknown;
		}
	}
}
