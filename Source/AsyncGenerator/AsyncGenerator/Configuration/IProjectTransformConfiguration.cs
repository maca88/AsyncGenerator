using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public interface IProjectTransformConfiguration
	{
		/// <summary>
		/// Set the name of the folder where all newly generated files will be stored
		/// </summary>
		IProjectTransformConfiguration AsyncFolder(string folderName);

		/// <summary>
		/// Set a function that can return a number of namespaces to import in a given document
		/// </summary>
		IProjectTransformConfiguration AdditionalDocumentNamespacesFunction(Func<CompilationUnitSyntax, IEnumerable<string>> func);

		/// <summary>
		/// Add a assembly reference to the project
		/// </summary>
		IProjectTransformConfiguration AddAssemblyReference(string assemblyPath);

		/// <summary>
		/// Set the parse options of the project
		/// </summary>
		IProjectTransformConfiguration ParseOptions(ParseOptions parseOptions);

		/// <summary>
		/// Wraps all generated code within the provided directive
		/// </summary>
		IProjectTransformConfiguration DirectiveForGeneratedCode(string directiveName);

		/// <summary>
		/// Indent all generated code using the provided indentation
		/// </summary>
		IProjectTransformConfiguration IndentationForGeneratedCode(string indentation);


	}
}
