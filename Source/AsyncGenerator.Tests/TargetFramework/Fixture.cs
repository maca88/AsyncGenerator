#if !NETCOREAPP
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Plugins;
using NUnit.Framework;

namespace AsyncGenerator.Tests.TargetFramework
{
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Test]
		public Task TestSolution()
		{
			var config = AsyncCodeConfiguration.Create()
				.ConfigureSolution(GetTestSolutionPath("MultiTargetFrameworks"), s => s
					.TargetFramework("net461")
					.ApplyChanges(false)
					.ConfigureProject("MultiTargetFrameworks", p => p
						.RegisterPlugin(new NUnitPlugin(false))));

			return AsyncCodeGenerator.GenerateAsync(config);
		}

		[Test]
		public Task TestProject()
		{
			var config = AsyncCodeConfiguration.Create()
				.ConfigureProject(GetTestProjectPath("MultiTargetFrameworks"), p => p
					.TargetFramework("net461")
					.ApplyChanges(false)
					.RegisterPlugin(new NUnitPlugin(false)));

			return AsyncCodeGenerator.GenerateAsync(config);
		}

		[Test]
		public Task TestYamlSolution()
		{
			var config = ConfigureByYaml(
$@"
solution:
  filePath: {GetTestSolutionPath("MultiTargetFrameworks")}
  targetFramework: net461
  applyChanges: false
  projects:
  - name: MultiTargetFrameworks
    registerPlugin:
    - type: AsyncGenerator.Core.Plugins.NUnitPlugin
      parameters:
      - name: createNewTypes
        value: false
      assemblyName: AsyncGenerator.Core
");

			return AsyncCodeGenerator.GenerateAsync(config);
		}

		[Test]
		public Task TestYamlProject()
		{
			var config = ConfigureByYaml(
$@"
projects:
- filePath: {GetTestProjectPath("MultiTargetFrameworks")}
  targetFramework: net461
  applyChanges: false
  registerPlugin:
  - type: AsyncGenerator.Core.Plugins.NUnitPlugin
    parameters:
    - name: createNewTypes
      value: false
    assemblyName: AsyncGenerator.Core
");

			return AsyncCodeGenerator.GenerateAsync(config);
		}

		[Test]
		public Task TestXmlSolution()
		{
			var config = ConfigureByXml(
$@"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Solution filePath=""{GetTestSolutionPath("MultiTargetFrameworks")}"">
    <ApplyChanges>false</ApplyChanges>
    <TargetFramework>net461</TargetFramework>
    <Projects>
      <Project name=""MultiTargetFrameworks"">
        <RegisterPlugin>
          <Plugin type=""AsyncGenerator.Core.Plugins.NUnitPlugin"" assemblyName=""AsyncGenerator.Core"">
            <Parameters>
              <Parameter name=""createNewTypes"" value=""false"" />
            </Parameters>
        </Plugin>
        </RegisterPlugin>
      </Project>
    </Projects>
  </Solution>
</AsyncGenerator>
");

			return AsyncCodeGenerator.GenerateAsync(config);
		}

		[Test]
		public Task TestXmlProject()
		{
			var config = ConfigureByXml(
$@"
<AsyncGenerator xmlns=""https://github.com/maca88/AsyncGenerator"">
  <Projects>
    <Project filePath=""{GetTestProjectPath("MultiTargetFrameworks")}"">
      <ApplyChanges>false</ApplyChanges>
      <TargetFramework>net461</TargetFramework>
      <RegisterPlugin>
        <Plugin type=""AsyncGenerator.Core.Plugins.NUnitPlugin"" assemblyName=""AsyncGenerator.Core"">
          <Parameters>
            <Parameter name=""createNewTypes"" value=""false"" />
          </Parameters>
        </Plugin>
      </RegisterPlugin>
    </Project>
  </Projects>
</AsyncGenerator>
");

			return AsyncCodeGenerator.GenerateAsync(config);
		}
	}
}
#endif