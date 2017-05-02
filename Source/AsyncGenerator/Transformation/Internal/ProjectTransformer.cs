using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
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
			var project = analyzationResult.Project;
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
				if (docResult.OriginalModified != null)
				{
					project = project.GetDocument(document.Document.Id).WithSyntaxRoot(docResult.OriginalModified).Project;
				}
				var folders = new List<string> { _configuration.AsyncFolder }.Union(document.Document.Folders);
				project = project.AddDocument(document.Document.Name, docResult.Transformed.GetText(Encoding.UTF8), folders, GetDocumentAsyncPath(document.Document)).Project;
			}
			result.Project = project;
			return result;
		}

		private string GetDocumentAsyncPath(Document document)
		{
			var path = "";
			for (var i = 0; i < document.Folders.Count; i++)
			{
				path += @"..\";
			}
			path += $@"{_configuration.AsyncFolder}\";
			path += string.Join(@"\", document.Folders);
			return $@"{Path.GetFullPath(Path.Combine(Path.GetDirectoryName(document.FilePath), path))}\{document.Name}";
		}
	}
}
