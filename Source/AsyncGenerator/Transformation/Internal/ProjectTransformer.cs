using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Logging;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal partial class ProjectTransformer
	{
		private readonly ILogger _logger;
		private readonly ProjectTransformConfiguration _configuration;

		public ProjectTransformer(ProjectTransformConfiguration configuration, ILoggerFactory loggerFactory)
		{
			_configuration = configuration;
			_logger = loggerFactory.GetLogger($"{nameof(AsyncGenerator)}.{nameof(ProjectTransformer)}");
		}

		public IProjectTransformationResult Transform(IProjectAnalyzationResult analyzationResult)
		{
			var result = new ProjectTransformationResult(analyzationResult.Project);

			void TransfromDocument(IDocumentAnalyzationResult document)
			{
				// Skip empty documents
				if (document.GlobalNamespace.Types.Count == 0 && document.GlobalNamespace.NestedNamespaces.Count == 0)
				{
					return;
				}
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
			_logger.Info("Generating documents started");
			if (_configuration.ConcurrentRun)
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
			_logger.Info("Generating documents completed");

			// Step 2: Modify the project by adding newly generated documents and optionally update the existing ones
			_logger.Info("Adding generated documents to the project started");
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
			_logger.Info("Adding generated documents to the project completed");
			
			return result;
		}

		private string GetDocumentAsyncPath(Document document)
		{
			var paths = new List<string> { Path.GetDirectoryName(document.FilePath) };
			for (var i = 0; i < document.Folders.Count; i++)
			{
				paths.Add("..");
			}
			paths.Add(_configuration.AsyncFolder);
			paths.AddRange(document.Folders);
			paths.Add(document.Name);
			return Path.GetFullPath(Path.Combine(paths.ToArray()));
		}
	}
}
