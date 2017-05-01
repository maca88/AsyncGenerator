using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AsyncGenerator.Transformation.Internal
{
	internal partial class ProjectTransformer
	{
		private readonly ProjectTransformConfiguration _configuration;

		public ProjectTransformer(ProjectTransformConfiguration configuration)
		{
			_configuration = configuration;
		}

		public IProjectTransformationResult Transform(IProjectAnalyzationResult analyzationResult)
		{
			var result = new ProjectTransformationResult(analyzationResult.Project);
			foreach (var document in analyzationResult.Documents)
			{
				var docResult = TransformDocument(document);
				result.Documents.Add(docResult);
				if (docResult.Transformed == null)
				{
					continue;
				}
				foreach (var transformer in _configuration.DocumentTransformers)
				{
					docResult.Transformed = transformer.Transform(docResult) ?? docResult.Transformed;
				}
			}
			return result;
		}
	}
}
