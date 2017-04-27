using System;
using System.Collections.Generic;
using System.IO;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectTransformConfiguration : IProjectTransformConfiguration
	{
		public string AsyncFolder { get; private set; } = "Async";

		public Func<CompilationUnitSyntax, IEnumerable<string>> AdditionalDocumentNamespaces { get; private set; }

		public HashSet<string> AssemblyReferences { get; } = new HashSet<string>();

		public ParseOptions ParseOptions { get; private set; }

		public ExpressionSyntax ConfigureAwaitArgument { get; private set; }

		public bool LocalFunctions { get; private set; }

		public List<Action<IProjectTransformationResult>> AfterTransformation { get; } = new List<Action<IProjectTransformationResult>>();

		IProjectTransformConfiguration IProjectTransformConfiguration.AsyncFolder(string folderName)
		{
			AsyncFolder = folderName ?? throw new ArgumentNullException(nameof(folderName));
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.ConfigureAwaitArgument(ExpressionSyntax value)
		{
			ConfigureAwaitArgument = value;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.LocalFunctions(bool enabled)
		{
			LocalFunctions = enabled;
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.AdditionalDocumentNamespaces(
			Func<CompilationUnitSyntax, IEnumerable<string>> func)
		{
			AdditionalDocumentNamespaces = func ?? throw new ArgumentNullException(nameof(func));
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
			ParseOptions = parseOptions ?? throw new ArgumentNullException(nameof(parseOptions));
			return this;
		}

		IProjectTransformConfiguration IProjectTransformConfiguration.AfterTransformation(Action<IProjectTransformationResult> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			AfterTransformation.Add(action);
			return this;
		}
	}
}
