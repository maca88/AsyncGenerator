using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public class ProjectTransformConfiguration : IProjectTransformConfiguration
	{
		public string AsyncFolder { get; private set; } = "Async";

		public Func<CompilationUnitSyntax, IEnumerable<string>> AdditionalDocumentNamespacesFunction { get; private set; }

		public HashSet<string> AssemblyReferences { get; } = new HashSet<string>();

		public ParseOptions ParseOptions { get; private set; }

		public string DirectiveForGeneratedCode { get; private set; }

		public string IndentationForGeneratedCode { get; private set; }

		IProjectTransformConfiguration IProjectTransformConfiguration.AsyncFolder(string folderName)
		{
			if (folderName == null)
			{
				throw new ArgumentNullException(nameof(folderName));
			}
			AsyncFolder = folderName;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.AdditionalDocumentNamespacesFunction(
			Func<CompilationUnitSyntax, IEnumerable<string>> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			AdditionalDocumentNamespacesFunction = func;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.AddAssemblyReference(string assemblyPath)
		{
			if (assemblyPath == null)
			{
				throw new ArgumentNullException(nameof(assemblyPath));
			}
			if (!File.Exists(assemblyPath))
			{
				throw new FileNotFoundException(assemblyPath);
			}
			AssemblyReferences.Add(assemblyPath);
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.ParseOptions(ParseOptions parseOptions)
		{
			if (parseOptions == null)
			{
				throw new ArgumentNullException(nameof(parseOptions));
			}
			ParseOptions = parseOptions;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.DirectiveForGeneratedCode(string directiveName)
		{
			if (directiveName == null)
			{
				throw new ArgumentNullException(nameof(directiveName));
			}
			DirectiveForGeneratedCode = directiveName;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.IndentationForGeneratedCode(string indentation)
		{
			if (indentation == null)
			{
				throw new ArgumentNullException(nameof(indentation));
			}
			IndentationForGeneratedCode = indentation;
			return this;
		}
	}
}
