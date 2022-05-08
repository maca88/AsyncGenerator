using System;
using System.Collections.Generic;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectTransformConfiguration : IFluentProjectTransformConfiguration, IProjectTransformConfiguration
	{
		private readonly IProjectConfiguration _projectConfiguration;

		public ProjectTransformConfiguration(IProjectConfiguration projectConfiguration)
		{
			_projectConfiguration = projectConfiguration;
			DocumentationComments = new ProjectDocumentationCommentConfiguration();
			PreprocessorDirectives = new ProjectPreprocessorDirectivesConfiguration();
		}

		public bool Enabled { get; private set; } = true;

		public string AsyncFolder { get; private set; } = "Async";

		public ExpressionSyntax ConfigureAwaitArgument { get; private set; }

		public bool LocalFunctions { get; private set; }

		public string AsyncLockFullTypeName { get; private set; }

		public string AsyncLockMethodName { get; private set; }

		public bool ConcurrentRun => _projectConfiguration.ConcurrentRun;

		public ProjectDocumentationCommentConfiguration DocumentationComments { get; }

		public ProjectPreprocessorDirectivesConfiguration PreprocessorDirectives { get; }

		public List<IMethodOrAccessorTransformer> MethodTransformers { get; } = new List<IMethodOrAccessorTransformer>();

		public List<IFunctionTransformer> FunctionTransformers { get; } = new List<IFunctionTransformer>();

		public List<ITypeTransformer> TypeTransformers { get; } = new List<ITypeTransformer>();

		public List<IDocumentTransformer> DocumentTransformers { get; } = new List<IDocumentTransformer>();

		public List<IFunctionReferenceTransformer> FunctionReferenceTransformers { get; } = new List<IFunctionReferenceTransformer>();

		public List<Action<IProjectTransformationResult>> AfterTransformation { get; } = new List<Action<IProjectTransformationResult>>();

		#region IFluentProjectTransformConfiguration

		IProjectDocumentationCommentConfiguration IProjectTransformConfiguration.DocumentationComments => DocumentationComments;

		IProjectPreprocessorDirectivesConfiguration IProjectTransformConfiguration.PreprocessorDirectives => PreprocessorDirectives;

		#endregion

		#region IFluentProjectTransformConfiguration

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.Disable()
		{
			Enabled = false;
			return this;
		}

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

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.ConfigureAwaitArgument(bool? value)
		{
			if (value == null)
			{
				ConfigureAwaitArgument = null;
				return this;
			}
			ConfigureAwaitArgument = value.Value
				? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
				: SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.LocalFunctions(bool enabled)
		{
			LocalFunctions = enabled;
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.AsyncLock(string fullTypeName, string methodName)
		{
			AsyncLockFullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
			AsyncLockMethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.DocumentationComments(Action<IFluentProjectDocumentationCommentConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(DocumentationComments);
			return this;
		}

		IFluentProjectTransformConfiguration IFluentProjectTransformConfiguration.PreprocessorDirectives(Action<IFluentProjectPreprocessorDirectivesConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(PreprocessorDirectives);
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
