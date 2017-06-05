using System;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectCompileConfiguration : IFluentProjectCompileConfiguration, IProjectCompileConfiguration
	{
		public ProjectCompileConfiguration(string outputPath)
		{
			OutputPath = outputPath;
		}

		public string OutputPath { get; }

		public string SymbolsPath { get; private set; }

		public string XmlDocumentationPath { get; private set; }

		#region IFluentProjectCompileConfiguration

		IFluentProjectCompileConfiguration IFluentProjectCompileConfiguration.SymbolsPath(string path)
		{
			SymbolsPath = path ?? throw new ArgumentNullException(nameof(path));
			return this;
		}

		IFluentProjectCompileConfiguration IFluentProjectCompileConfiguration.XmlDocumentationPath(string path)
		{
			XmlDocumentationPath = path ?? throw new ArgumentNullException(nameof(path));
			return this;
		}

		#endregion
	}
}
