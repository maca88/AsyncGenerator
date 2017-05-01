using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface IProjectCompileConfiguration
	{
		string OutputPath { get; }

		string SymbolsPath { get; }

		string XmlDocumentationPath { get; }
	}
}
