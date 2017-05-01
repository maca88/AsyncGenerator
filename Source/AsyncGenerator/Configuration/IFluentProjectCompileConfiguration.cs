using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface IFluentProjectCompileConfiguration
	{
		/// <summary>
		/// Set the path where the symbols will be placed 
		/// </summary>
		IFluentProjectCompileConfiguration SymbolsPath(string path);

		/// <summary>
		/// Set the path where the xml documentation will be placed 
		/// </summary>
		IFluentProjectCompileConfiguration XmlDocumentationPath(string path);
	}
}
