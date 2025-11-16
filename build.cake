#tool nuget:?package=NUnit.ConsoleRunner&version=3.17.0
#tool nuget:?package=NuGet.CommandLine&version=5.9.1
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var netfx = Argument("netfx", "net472");
var netcore = Argument("netcore", "net8.0");
var netcoreVersion = Argument("netcoreVersion", "");
var nugetApiKey = Argument("nugetApiKey", "");

//////////////////////////////////////////////////////////////////////
// CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDirs = new List<string>()
{
    Directory("./Source/AsyncGenerator/bin") + Directory(configuration),
    Directory("./Source/AsyncGenerator.CommandLine/bin") + Directory(configuration),
    Directory("./Source/AsyncGenerator.Tool/bin") + Directory(configuration),
    Directory("./Source/AsyncGenerator.Configuration.Yaml/bin") + Directory(configuration),
    Directory("./Source/AsyncGenerator.Core/bin") + Directory(configuration)
};

void SetupTestFramework(string framework)
{
    string content;
    if (FileExists("Common.dev.props"))
    {
        content = System.IO.File.ReadAllText("Common.dev.props");
        content = System.Text.RegularExpressions.Regex.Replace(content, @"(<TestTargetFramework>)([\w\.;]+)(</TestTargetFramework>)", $"$1{framework}$3");
        content = System.Text.RegularExpressions.Regex.Replace(content, @"(<AppTargetFrameworks>)([\w\.;]+)(</AppTargetFrameworks>)", $"$1{framework}$3");
        content = System.Text.RegularExpressions.Regex.Replace(content, @"(<LibTargetFrameworks>)([\w\.;]+)(</LibTargetFrameworks>)", $"$1{framework}$3");
        content = System.Text.RegularExpressions.Regex.Replace(content, @"(<ToolTargetFrameworks>)([\w\.;]+)(</ToolTargetFrameworks>)", $"$1{framework}$3");
    }
    else
    {
        content =
$@"<Project>
  <PropertyGroup>
    <TestTargetFramework>{framework}</TestTargetFramework>
    <AppTargetFrameworks>{framework}</AppTargetFrameworks>
    <LibTargetFrameworks>{framework}</LibTargetFrameworks>
    <ToolTargetFrameworks>{framework}</ToolTargetFrameworks>
  </PropertyGroup>
</Project>";
    }
    
    System.IO.File.WriteAllText("Common.dev.props", content);
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    foreach(var buildDir in buildDirs)
    {
        CleanDirectory(buildDir);
    }
});

Task("SetupTestFramework")
    .Does(() =>
{
    SetupTestFramework(netfx);
});

Task("SetupTestFrameworkCore")
    .IsDependentOn("ClearGlobalJson")
    .Does(() =>
{
    if (!string.IsNullOrEmpty(netcoreVersion))
    {
        var content = string.Format(
@"{{
  ""sdk"": {{
    ""version"": ""{0}"",
    ""rollForward"": ""latestFeature""
  }}
}}", netcoreVersion);
        System.IO.File.WriteAllText("global.json", content);
    }

    SetupTestFramework(netcore);
});

Task("ClearTestFramework")
    .Does(() =>
{
    if (FileExists("Common.dev.props"))
    {
        System.IO.File.Delete("Common.dev.props");
    }
});

Task("ClearGlobalJson")
    .Does(() =>
{
    if (FileExists("global.json"))
    {
        System.IO.File.Delete("global.json");
    }
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./Source/AsyncGenerator.sln", new NuGetRestoreSettings
    {
        ConfigFile = "./Nuget.config"
    });
    var testSolutions = GetFiles("./Source/AsyncGenerator.TestProjects/*.sln");
    foreach(var testSolution in testSolutions)
    {
        Information("Restoring {0}", testSolution);
        NuGetRestore(testSolution, new NuGetRestoreSettings
        {
            ConfigFile = "./Nuget.config"
        });
    }
});

Task("RestoreCore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./Source/AsyncGenerator.sln", new DotNetRestoreSettings
    {
        ConfigFile = "./Nuget.config"
    });
    var testSolutions = GetFiles("./Source/AsyncGenerator.TestProjects/*.sln");
    foreach(var testSolution in testSolutions)
    {
        Information("Restoring {0}", testSolution);
        DotNetRestore(testSolution.FullPath, new DotNetRestoreSettings
        {
            ConfigFile = "./Nuget.config"
        });
    }
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("./Source/AsyncGenerator.sln", settings =>
        settings.SetConfiguration(configuration));
});

Task("BuildCore")
    .IsDependentOn("RestoreCore")
    .Does(() =>
{
    DotNetBuild("./Source/AsyncGenerator.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore"),
    });
});

Task("Test")
    .IsDependentOn("SetupTestFramework")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./Source/AsyncGenerator.Tests/bin/" + configuration + $"/{netfx}/*.Tests.dll", new NUnit3Settings
    {
        NoResults = true
    });
});

Task("TestCore")
    .IsDependentOn("SetupTestFrameworkCore")
    .IsDependentOn("BuildCore")
    .Does(() =>
{
    DotNetTest("./Source/AsyncGenerator.Tests/AsyncGenerator.Tests.csproj", new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    });
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("CleanPackages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("Pack")
    .IsDependentOn("ClearTestFramework")
    .IsDependentOn("BuildCore") // We have to build in order to include dlls for the AsyncGenerator.CommandLine project
    .IsDependentOn("CleanPackages")
    .Description("Creates NuGet packages")
    .Does(() =>
{
    CreateDirectory(PACKAGE_DIR);

    var projects = new string[]
    {
        "Source/AsyncGenerator.Core/AsyncGenerator.Core.csproj",
        "Source/AsyncGenerator.Configuration.Yaml/AsyncGenerator.Configuration.Yaml.csproj",
        "Source/AsyncGenerator/AsyncGenerator.csproj",
        "Source/AsyncGenerator.CommandLine/AsyncGenerator.CommandLine.csproj"
    };

    foreach(var project in projects)
    {
        DotNetBuild(project, new DotNetBuildSettings {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("/t:pack")
                .Append("/p:PackageOutputPath=\"" + PACKAGE_DIR + "\"")
        });
    }

    DotNetPack("Source/AsyncGenerator.Tool/AsyncGenerator.Tool.csproj", new DotNetPackSettings {
        Configuration = configuration,
        OutputDirectory = PACKAGE_DIR,
        IncludeSymbols = false,
        ArgumentCustomization = arg => arg.Append("/p:PackAsTool=true")
    });
});

Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        DotNetNuGetPush(package, new DotNetNuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = nugetApiKey,
            SkipDuplicate = true
        });
    }
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("TestCore");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
