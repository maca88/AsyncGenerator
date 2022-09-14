using System;
using System.Collections.Generic;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAsyncExtensionMethodsConfiguration : IFluentProjectAsyncExtensionMethodsConfiguration
	{
		public Dictionary<string, HashSet<string>> ProjectFiles { get; } = new Dictionary<string, HashSet<string>>();
		
		public Dictionary<string, HashSet<string>> AssemblyTypes { get; } = new Dictionary<string, HashSet<string>>();

		IFluentProjectAsyncExtensionMethodsConfiguration IFluentProjectAsyncExtensionMethodsConfiguration.ProjectFile(string projectName, string fileName)
		{
			if (projectName == null)
			{
				throw new ArgumentNullException(nameof(projectName));
			}
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			if (!ProjectFiles.ContainsKey(projectName))
			{
				ProjectFiles.Add(projectName, new HashSet<string>());
			}
			ProjectFiles[projectName].Add(fileName);
			return this;
		}
		
		IFluentProjectAsyncExtensionMethodsConfiguration IFluentProjectAsyncExtensionMethodsConfiguration.ExternalType(string assemblyName, string fullTypeName)
		{
			if (assemblyName == null)
			{
				throw new ArgumentNullException(nameof(assemblyName));
			}
			if (fullTypeName == null)
			{
				throw new ArgumentNullException(nameof(fullTypeName));
			}

			if (!AssemblyTypes.ContainsKey(assemblyName))
			{
				AssemblyTypes.Add(assemblyName, new HashSet<string>());
			}
			AssemblyTypes[assemblyName].Add(fullTypeName);
			return this;
		}
	}
}
