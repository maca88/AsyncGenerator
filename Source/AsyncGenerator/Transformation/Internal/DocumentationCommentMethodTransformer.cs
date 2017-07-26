using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	internal class DocumentationCommentMethodTransformer : IMethodOrAccessorTransformer
	{
		private IProjectDocumentationCommentConfiguration _configuration;
		private bool _isEnabled;

		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			_configuration = configuration.TransformConfiguration.DocumentationComments;
			_isEnabled =
				_configuration.CanRemoveMethodRemarks != null ||
				_configuration.CanRemoveMethodSummary != null ||
				_configuration.AddOrReplaceMethodRemarks != null ||
				_configuration.AddOrReplaceMethodSummary != null
				;
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!_isEnabled)
			{
				return MethodTransformerResult.Skip;
			}

			var leadingTrivia = methodTransformResult.Transformed.GetLeadingTrivia();
			var documentation = leadingTrivia.FirstOrDefault(o => o.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
			var documentationNode = (DocumentationCommentTriviaSyntax)documentation.GetStructure();
			var newDocumentationNode = documentationNode ??
			                           DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia);

			// Summary
			newDocumentationNode = ProcessTag(
				newDocumentationNode,
				methodTransformResult,
				"summary",
				_configuration.AddOrReplaceMethodSummary,
				_configuration.CanRemoveMethodSummary, true);

			// Remarks
			newDocumentationNode = ProcessTag(
				newDocumentationNode, 
				methodTransformResult, 
				"remarks", 
				_configuration.AddOrReplaceMethodRemarks, 
				_configuration.CanRemoveMethodRemarks, false);


			var transformed = methodTransformResult.Transformed;

			if (documentationNode != null)
			{
				transformed = transformed.ReplaceNode(documentationNode, newDocumentationNode);
			}
			else
			{
				// We have to append our documentation before the whitespace trivia
				var whitespaceIndex = leadingTrivia.IndexOf(SyntaxKind.WhitespaceTrivia);
				if (whitespaceIndex <= 0)
				{
					whitespaceIndex = 0;
				}
				transformed = transformed.WithLeadingTrivia(transformed.GetLeadingTrivia().Insert(whitespaceIndex, Trivia(newDocumentationNode)));
			}

			return MethodTransformerResult.Update(transformed);
		}

		private DocumentationCommentTriviaSyntax ProcessTag(
			DocumentationCommentTriviaSyntax documentationNode,
			IMethodOrAccessorTransformationResult methodTransformResult, 
			string tagName, 
			Func<IMethodSymbol, string> addOrReplace, 
			Predicate<IMethodSymbol> canRemove,
			bool prepend)
		{
			if (addOrReplace == null && canRemove == null)
			{
				return documentationNode;
			}
			var methodSymbol = methodTransformResult.AnalyzationResult.Symbol;
			var tagContent = addOrReplace?.Invoke(methodSymbol);
			var removeTag = canRemove?.Invoke(methodSymbol);
			var tagNode = documentationNode.Content.OfType<XmlElementSyntax>()
				.FirstOrDefault(o => o.StartTag.Name.ToString() == tagName);

			if (!string.IsNullOrEmpty(tagContent))
			{
				var indent = methodTransformResult.LeadingWhitespaceTrivia.ToFullString();
				var eol = methodTransformResult.EndOfLineTrivia.ToFullString();
				var tagComment = CreateTagContent(tagName, tagContent, indent, eol);

				// When prepending a new tag before an existing tag we have to add the indentation at the end otherwise on start
				if (prepend && tagNode == null && documentationNode.Content.Any())
				{
					var lastComment = tagComment.Content.Last();
					tagComment = tagComment.ReplaceNode(lastComment, lastComment
						.WithTrailingTrivia(lastComment.GetTrailingTrivia()
							.Add(methodTransformResult.LeadingWhitespaceTrivia)));
				}
				else
				{
					var startComment = tagComment.Content.First();
					tagComment = tagComment.ReplaceNode(startComment, startComment
						.WithLeadingTrivia(DocumentationCommentExterior($"{indent}///")));
				}

				if (tagNode != null)
				{
					documentationNode = documentationNode.ReplaceNode(tagNode, tagComment.Content.OfType<XmlElementSyntax>().First());
				}
				else
				{
					documentationNode = documentationNode.WithContent(prepend 
						? documentationNode.Content.InsertRange(0, tagComment.Content) 
						: documentationNode.Content.AddRange(tagComment.Content));
				}
			}
			else if (removeTag == true && tagNode != null)
			{
				// We need to remove the "///" XmlText and the tag itself
				var index = documentationNode.Content.IndexOf(tagNode);
				documentationNode = documentationNode.RemoveNode(tagNode, SyntaxRemoveOptions.KeepNoTrivia);
				documentationNode = documentationNode.RemoveNode(documentationNode.Content[index - 1], SyntaxRemoveOptions.KeepNoTrivia);
			}
			return documentationNode;
		}

		private DocumentationCommentTriviaSyntax CreateTagContent(string tagName, string content, string indent, string eol)
		{
			var lines = content
				.Replace("\r\n", "\r")
				.Replace("\n\r", "\r")
				.Split('\r', '\n')
				.Select(o => o.Trim())
				.Where(o => !string.IsNullOrEmpty(o))
				.ToList();

			for (var i = 0; i < lines.Count; i++)
			{
				var line = lines[i];
				if (!line.StartsWith("///"))
				{
					lines[i] = $"/// {line}";
				}
				lines[i] = indent + lines[i];
			}

			var tag = $"/// <{tagName}>{eol}";
			tag = lines.Aggregate(tag, (current, line) => current + line + eol);
			tag += $"{indent}/// </{tagName}>{eol}";

			return (DocumentationCommentTriviaSyntax)CSharpSyntaxTree.ParseText(tag)
				.GetRoot()
				.DescendantTrivia()
				.First(o => o.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
				.GetStructure();
		}
	}
}
