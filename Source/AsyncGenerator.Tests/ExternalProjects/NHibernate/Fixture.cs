using System.IO;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
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
	[Explicit, TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task Test()
		{
			var configPath = Path.GetFullPath(Path.Combine(GetExternalProjectDirectory("NHibernate"), "src", "AsyncGenerator.yml"));
			//var configPath = Path.Combine(@"C:\Workspace\Git\nhibernate-core\src", "AsyncGenerator.yml");
			var config = AsyncCodeConfiguration.Create()
				.ConfigureFromFile<YamlFileConfigurator>(configPath);
			return AsyncCodeGenerator.GenerateAsync(config);
		}
	}
}
