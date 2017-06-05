using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using log4net;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal partial class ProjectTransformer
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProjectTransformer));
		private readonly ProjectTransformConfiguration _configuration;

		public ProjectTransformer(ProjectTransformConfiguration configuration)
		{
			_configuration = configuration;
		}

		public IProjectTransformationResult Transform(IProjectAnalyzationResult analyzationResult)
		{
			var result = new ProjectTransformationResult(analyzationResult.Project);

			void TransfromDocument(IDocumentAnalyzationResult document)
			{
				var docResult = TransformDocument(document);
				result.Documents.Add(docResult);
				if (docResult.Transformed == null)
				{
					return;
				}
				foreach (var transformer in _configuration.DocumentTransformers)
				{
					docResult.Transformed = transformer.Transform(docResult) ?? docResult.Transformed;
				}
			}

			// Step 1: Transform all documents
			Logger.Info("Generating documents started");
			if (_configuration.RunInParallel)
			{
				Parallel.ForEach(analyzationResult.Documents, TransfromDocument);
			}
			else
			{
				foreach (var document in analyzationResult.Documents)
				{
					TransfromDocument(document);
				}
			}
			Logger.Info("Generating documents completed");

			// Step 2: Modify the project by adding newly generated documents and optionally update the existing ones
			Logger.Info("Adding generated documents to the project started");
			var project = analyzationResult.Project;
			foreach (var docResult in result.Documents)
			{
				if (docResult.Transformed == null)
				{
					continue;
				}
				var document = docResult.AnalyzationResult;
				if (docResult.OriginalModified != null)
				{
					project = project.GetDocument(document.Document.Id).WithSyntaxRoot(docResult.OriginalModified).Project;
				}
				var folders = new List<string> { _configuration.AsyncFolder }.Union(document.Document.Folders);
				project = project.AddDocument(document.Document.Name, docResult.Transformed.GetText(Encoding.UTF8), folders, GetDocumentAsyncPath(document.Document)).Project;
			}
			result.Project = project;
			Logger.Info("Adding generated documents to the project completed");
			
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
