#tool nuget:?package=NUnit.ConsoleRunner&version=3.6.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

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
    Directory("./Source/AsyncGenerator.Configuration.Yaml/bin") + Directory(configuration),
    Directory("./Source/AsyncGenerator.Core/bin") + Directory(configuration)
};

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

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./Source/AsyncGenerator.sln", new NuGetRestoreSettings()
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

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    MSBuild("./Source/AsyncGenerator.sln", settings =>
        settings.SetConfiguration(configuration));
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./Source/**/bin/" + configuration + "/net461/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("Clean-Packages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("Pack-NuGet-Packages")
    .IsDependentOn("Clean-Packages")
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
        MSBuild(project, new MSBuildSettings {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("/t:pack")
                .Append("/p:PackageOutputPath=\"" + PACKAGE_DIR + "\"")
        });
    }
});
    
Task("Publish-NuGet-Packages")
    //.IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Pack-NuGet-Packages")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        NuGetPush(package, new NuGetPushSettings()
        {
            Source = "https://www.nuget.org/api/v2/package"
        });
    }
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");
    
Task("Publish")
    .IsDependentOn("Publish-NuGet-Packages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
