namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectCompileConfiguration
	{
		string OutputPath { get; }

		string SymbolsPath { get; }

		string XmlDocumentationPath { get; }
	}
}
