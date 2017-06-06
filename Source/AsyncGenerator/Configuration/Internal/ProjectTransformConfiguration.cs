using System;
using System.Collections.Generic;
using System.IO;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Plugins;
using AsyncGenerator.Transformation;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectTransformConfiguration : IFluentProjectTransformConfiguration, IProjectTransformConfiguration
	{
		private readonly IProjectConfiguration _projectConfiguration;

		public ProjectTransformConfiguration(IProjectConfiguration projectConfiguration)
		{
			_projectConfiguration = projectConfiguration;
		}

		public bool Enabled { get; private set; } = true;

		public string AsyncFolder { get; private set; } = "Async";

		public ExpressionSyntax ConfigureAwaitArgument { get; private set; }

		public bool LocalFunctions { get; private set; }

		public string AsyncLockFullTypeName { get; private set; }

		public string AsyncLockMethodName { get; private set; }

		public bool ConcurrentRun => _projectConfiguration.ConcurrentRun;

		public List<IMethodTransformer> MethodTransformers { get; } = new List<IMethodTransformer>();

		public List<IDocumentTransformer> DocumentTransformers { get; } = new List<IDocumentTransformer>();

		public List<Action<IProjectTransformationResult>> AfterTransformation { get; } = new List<Action<IProjectTransformationResult>>();

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
