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
    NuGetRestore("./Source/AsyncGenerator.sln");
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
    NUnit3("./Source/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
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
    .IsDependentOn("Build")
    .IsDependentOn("Clean-Packages")
    .Description("Creates NuGet packages")
    .Does(() =>
{
    CreateDirectory(PACKAGE_DIR);
    NuGetPack("Source/AsyncGenerator.Core/AsyncGenerator.Core.csproj", new NuGetPackSettings()
    {
        OutputDirectory = PACKAGE_DIR,
        Symbols = true,
        ArgumentCustomization = args => args
            .Append("-Prop Configuration=" + configuration)
    });
    NuGetPack("Source/AsyncGenerator.Configuration.Yaml/AsyncGenerator.Configuration.Yaml.csproj", new NuGetPackSettings()
    {
        OutputDirectory = PACKAGE_DIR,
        Symbols = true,
        ArgumentCustomization = args => args
            .Append("-Prop Configuration=" + configuration)
    });
    NuGetPack("Source/AsyncGenerator/AsyncGenerator.csproj", new NuGetPackSettings()
    {
        OutputDirectory = PACKAGE_DIR,
        Symbols = true,
        ArgumentCustomization = args => args
            .Append("-Prop Configuration=" + configuration)
    });
    NuGetPack("Source/AsyncGenerator.CommandLine/AsyncGenerator.CommandLine.csproj", new NuGetPackSettings()
    {
        OutputDirectory = PACKAGE_DIR,
        ArgumentCustomization = args => args
            .Append("-Tool")
            .Append("-Prop Configuration=" + configuration)
    });
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
