using System;
using System.Collections.Generic;
using System.IO;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Plugins;
using AsyncGenerator.Transformation;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectTransformConfiguration : IFluentProjectTransformConfiguration, IProjectTransformConfiguration
	{
		public string AsyncFolder { get; private set; } = "Async";

		public Func<CompilationUnitSyntax, IEnumerable<string>> AdditionalDocumentNamespaces { get; private set; }

		public HashSet<string> AssemblyReferences { get; } = new HashSet<string>();

		public ParseOptions ParseOptions { get; private set; }

		public ExpressionSyntax ConfigureAwaitArgument { get; private set; }

		public bool LocalFunctions { get; private set; }

		public string AsyncLockFullTypeName { get; private set; }

		public string AsyncLockMethodName { get; private set; }

		public List<IMethodTransformer> MethodTransformers { get; } = new List<IMethodTransformer>();

		public List<IDocumentTransformer> DocumentTransformers { get; } = new List<IDocumentTransformer>();

		public List<Action<IProjectTransformationResult>> AfterTransformation { get; } = new List<Action<IProjectTransformationResult>>();

		#region IFluentProjectTransformConfiguration

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AsyncFolder(string folderName)
		{
			AsyncFolder = folderName ?? throw new ArgumentNullException(nameof(folderName));
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.ConfigureAwaitArgument(ExpressionSyntax value)
		{
			ConfigureAwaitArgument = value;
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.LocalFunctions(bool enabled)
		{
			LocalFunctions = enabled;
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AdditionalDocumentNamespaces(
			Func<CompilationUnitSyntax, IEnumerable<string>> func)
		{
			AdditionalDocumentNamespaces = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AddAssemblyReference(string assemblyPath)
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

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.ParseOptions(ParseOptions parseOptions)
		{
			ParseOptions = parseOptions ?? throw new ArgumentNullException(nameof(parseOptions));
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AsyncLock(string fullTypeName, string methodName)
		{
			AsyncLockFullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
			AsyncLockMethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AfterTransformation(Action<IProjectTransformationResult> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			AfterTransformation.Add(action);
			return this;
		}

		#endregion

	}
}
