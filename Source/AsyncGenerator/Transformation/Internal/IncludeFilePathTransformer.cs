using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// A transformer that rewrites the file path on include xml tags of a document
	/// </summary>
	internal class IncludeFilePathTransformer : IDocumentTransformer
	{
		private string _asyncFolderPath;

		public CompilationUnitSyntax Transform(IDocumentTransformationResult result)
		{
			var rootNode = result.Transformed;

			var annotations = new List<string>();

			var includeTagSpans = rootNode.DescendantNodes(descendIntoTrivia: true)
				.OfType<XmlEmptyElementSyntax>()
				.Where(o => o.Name.ToString() == "include")
				.Where(o => o.Attributes.OfType<XmlTextAttributeSyntax>().Any(a => a.Name.ToString() == "file"))
				.Select(o => o.Span)
				.ToList();

			if (!includeTagSpans.Any())
			{
				return rootNode;
			}

			foreach (var includeTagSpan in includeTagSpans)
			{
				var includeTag = rootNode.DescendantNodes(descendIntoTrivia: true).First(o => o.Span == includeTagSpan);
				var annotation = Guid.NewGuid().ToString();
				annotations.Add(annotation);
				rootNode = rootNode.ReplaceNode(includeTag, includeTag.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
			}
			var document = result.AnalyzationResult.Document;
			// First we need to get to the root of the project and then we can append the folder of the document
			var relPathToOrigin = _asyncFolderPath.Split('\\').Aggregate("", (current, folder) => current + @"..\");
			for (var i = 0; i < document.Folders.Count; i++)
			{
				relPathToOrigin += @"..\";
			}
			relPathToOrigin += string.Join(@"\", document.Folders);

			foreach (var annotation in annotations)
			{
				var includeTag = rootNode.GetAnnotatedNodes(annotation).OfType<XmlEmptyElementSyntax>().First();
				var fileAttr = includeTag.Attributes.OfType<XmlTextAttributeSyntax>().First(o => o.Name.ToString() == "file");
				// There should be only one xml text literal token
				var textToken = fileAttr.TextTokens.First();
				var newPath = relPathToOrigin + @"\" + textToken.ValueText;
				var newFileAttr = fileAttr.WithTextTokens(
					TokenList(XmlTextLiteral(
						textToken.LeadingTrivia,
						newPath,
						newPath,
						textToken.TrailingTrivia)));
				rootNode = rootNode.ReplaceNode(includeTag,
					includeTag.WithAttributes(
						includeTag.Attributes
							.Replace(fileAttr, newFileAttr)
					));
			}
			return rootNode;
		}

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_asyncFolderPath = configuration.TransformConfiguration.AsyncFolder;
			return Task.CompletedTask;
		}
	}
}
