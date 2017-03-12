using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public class UnitTest1
	{
		[Test]
		public void TestMethod1()
		{
			//ID of the package to be looked up
			string packageID = "EntityFramework";

			//Connect to the official package repository
			IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

			//Get the list of all NuGet packages with ID 'EntityFramework'       
			var package = repo.FindPackagesById(packageID)
				.First(o => o.IsLatestVersion);
			//.First(o => o.Version.ToFullString() == "5.0.0");


			//Initialize the package manager
			var path = @"C:\Workspace\Git\AsyncGenerator\Source\AsyncGenerator\packages";
			var packageManager = new PackageManager(repo, path);

			//Download and unzip the package
			packageManager.InstallPackage(package, false, false);
		}

	}
}
