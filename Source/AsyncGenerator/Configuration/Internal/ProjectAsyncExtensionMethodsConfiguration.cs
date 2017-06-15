using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAsyncExtensionMethodsConfiguration : IFluentProjectAsyncExtensionMethodsConfiguration
	{
		public Dictionary<string, HashSet<string>> ProjectFiles { get; } = new Dictionary<string, HashSet<string>>();

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
	}
}
