using Microsoft.CodeAnalysis.CSharp;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectParseConfiguration
	{
		/// <summary>
		/// Adds a preprocessor symbol name to the project
		/// </summary>
		IFluentProjectParseConfiguration AddPreprocessorSymbolName(string value);

		/// <summary>
		/// Removes a preprocessor symbol name from the project
		/// </summary>
		IFluentProjectParseConfiguration RemovePreprocessorSymbolName(string value);

		/// <summary>
		/// Specifies the language version
		/// </summary>
		IFluentProjectParseConfiguration LanguageVersion(LanguageVersion value);
	}
}
