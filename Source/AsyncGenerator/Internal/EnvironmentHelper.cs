#if ENV
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

#if NETCOREAPP || NET472

namespace AsyncGenerator.Internal
{
	internal static class EnvironmentHelper
	{
		private static readonly string[] MsBuildAssemblies =
		{
			"Microsoft.Build",
			"Microsoft.Build.Framework",
			"Microsoft.Build.Tasks.Core",
			"Microsoft.Build.Utilities.Core"
		};

		private static readonly string[] NuGetAssemblies =
		{
			"NuGet.Common",
			"NuGet.Configuration",
			"NuGet.DependencyResolver.Core",
			"NuGet.Frameworks",
			"NuGet.LibraryModel",
			"NuGet.Packaging.Core",
			"NuGet.Packaging",
			"NuGet.ProjectModel",
			"NuGet.Protocol",
			"NuGet.Versioning"
		};

		/// <summary>
		/// Setup the environment in order MSBuild to work
		/// </summary>
		public static void Setup()
		{
#if NET472
			if (IsMono)
			{
				SetupMsBuildPath(() =>
				{
					return GetMonoMsBuildPath(monoDir =>
					{
						Environment.SetEnvironmentVariable("MSBuildExtensionsPath", Path.Combine(monoDir, "xbuild"));
					});
				});
				var msbuildPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH"));
				foreach (var assembly in MsBuildAssemblies)
				{
					var src = Path.GetFullPath(Path.Combine(msbuildPath, $"{assembly}.dll"));
					var dest = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assembly}.dll"));
					File.Copy(src, dest, true);
				}
				// On OSX mono 5.10.0 NuGet assemblies are not loaded, use a custom resolver that loads them.
				var sdkPath = Path.Combine(msbuildPath, "Sdks", "Microsoft.NET.Sdk", "tools", "net46");
				AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
				{
					var assemblyName = new AssemblyName(eventArgs.Name);
					if (!NuGetAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase))
					{
						return null;
					}
					var path = Path.Combine(sdkPath, $"{assemblyName.Name}.dll");
					return File.Exists(path) ? Assembly.LoadFile(Path.Combine(sdkPath, $"{assemblyName.Name}.dll")) : null;
				};
				return;
			}

			var vsInstallDir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
			if (!string.IsNullOrEmpty(vsInstallDir) && Directory.Exists(vsInstallDir))
			{
				return;
			}
#endif
			var instance = MSBuildLocator.QueryVisualStudioInstances()
				.OrderByDescending(o => o.Version)
				.FirstOrDefault();

#if NETCOREAPP
			AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
			{
				var assemblyName = new AssemblyName(eventArgs.Name);
#if NETCOREAPP2_1
				// Workaround for .NET Core 2.2 - Redirect Newtonsoft.Json version 9.0.0 requested from Microsoft.Build.NuGetSdkResolver
				// to 10.0.3 which is provided with the framework
				if ("Newtonsoft.Json" == assemblyName.Name)
#else
				if (NuGetAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase))
#endif
				{
					var path = Path.Combine(instance.MSBuildPath, $"{assemblyName.Name}.dll");
					return File.Exists(path) ? Assembly.LoadFrom(path) : null;
				}

				return null;
			};
#endif

			if (instance != null)
			{
				MSBuildLocator.RegisterInstance(instance);
			}
			else
			{
				throw new InvalidOperationException(
					"Visual Studio installation directory was not found. " +
					"Install Visual Studio or set the environment variable VSINSTALLDIR");
			}

#if NET472
			Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.VisualStudioRootPath);
#endif
#if NETCOREAPP2_1
			// Hack to be able to open an project that have one or more project references
			var projectBuildManagerType = typeof(MSBuildProjectLoader).Assembly.GetType("Microsoft.CodeAnalysis.MSBuild.Build.ProjectBuildManager");
			var field = projectBuildManagerType.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
				.FirstOrDefault(o => o.Name == "s_defaultGlobalProperties");
			if (field != null)
			{
				var globalProperties = (ImmutableDictionary<string, string>)field.GetValue(null);
				field.SetValue(null, globalProperties.SetItem("BuildingInsideVisualStudio", "false"));
			}
#endif
		}

		public static  bool IsMono => Type.GetType("Mono.Runtime") != null;

		// On Mono RuntimeInformation.IsOSPlatform will always retrun true for Windows
		public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
#if NETCOREAPP
		public static string GetConfigurationFilePath()
		{
			var name = AppDomain.CurrentDomain.FriendlyName;
			// On .NET Core FriendlyName as only the assembly name without the extension
			name += ".dll.config";
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
			return File.Exists(path) ? path : null;
		}
#endif
		public static string GetMonoMsBuildPath(Action<string> monoDirectoryAction = null)
		{
			// Get the sdk path by using the Mono runtime assembly location
			// Default locations:
			// Windows -> C:\Program Files (x86)\Mono\lib\mono\4.5\mscorlib.dll
			// Linux -> /usr/lib/mono/4.5/mscorlib.dll
			// OSX -> /Library/Frameworks/Mono.framework/Versions/5.2.0/lib/mono/4.5/mscorlib.dll
			// MSBuild.dll is then located:
			// Windows -> C:\Program Files (x86)\Mono\lib\mono\msbuild\15.0\bin\MSBuild.dll
			// Linux -> /usr/lib/mono/msbuild/15.0/bin/MSBuild.dll
			// OSX -> /Library/Frameworks/Mono.framework/Versions/5.2.0/lib/mono/msbuild/15.0/bin/MSBuild.dll

			var assembly = typeof(System.Runtime.GCSettings).Assembly;
			var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
			var directoryInfo = new DirectoryInfo(assemblyDirectory).Parent; // get mono directory
			monoDirectoryAction?.Invoke(directoryInfo.FullName);
			var msBuildPath = Path.Combine(directoryInfo.FullName, "msbuild", "15.0", "bin", "MSBuild.dll");
			return File.Exists(msBuildPath) ? msBuildPath : null;
		}

		private static void SetupMsBuildPath(Func<string> getMsBuildPathFunc)
		{
			var msbuildPath = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");
			if (!string.IsNullOrEmpty(msbuildPath) && File.Exists(msbuildPath))
			{
				return;
			}
			msbuildPath = getMsBuildPathFunc();
			if (msbuildPath == null)
			{
				throw new InvalidOperationException(
					"Environment variable MSBUILD_EXE_PATH is not set or is set incorrectly. " +
					"Please set MSBUILD_EXE_PATH to point at MSBuild.dll.");
			}
			Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildPath);
		}
	}
}

#endif
#endif