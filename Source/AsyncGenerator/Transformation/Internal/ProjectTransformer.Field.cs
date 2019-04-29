using System;
using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	internal partial class ProjectTransformer
	{
		internal FieldTransformationResult TransformField(
			BaseFieldDeclarationSyntax fieldNode,
			bool canCopy,
			FieldTransformationResult result,
			ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			var analyzeResult = result.AnalyzationResult;
			var startRootSpan = analyzeResult.Node.SpanStart;

			// Calculate whitespace method trivias
			result.EndOfLineTrivia = fieldNode.GetEndOfLine();
			result.LeadingWhitespaceTrivia = fieldNode.GetLeadingWhitespace();
			result.IndentTrivia = fieldNode.GetIndent(result.LeadingWhitespaceTrivia, typeMetadata.LeadingWhitespaceTrivia);

			if (analyzeResult.Variables.All(o => o.Conversion == FieldVariableConversion.Ignore || o.Conversion == FieldVariableConversion.Copy && !canCopy))
			{
				return result;
			}

			fieldNode = fieldNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startRootSpan -= fieldNode.SpanStart;

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in analyzeResult.TypeReferences.Where(o => o.TypeAnalyzationResult.Conversion == TypeConversion.NewType))
			{
				var reference = typeReference.ReferenceLocation;
				var refSpanStart = reference.Location.SourceSpan.Start - startRootSpan;
				var nameNode = fieldNode.GetSimpleName(refSpanStart, reference.Location.SourceSpan.Length, typeReference.IsCref);
				var annotation = Guid.NewGuid().ToString();
				fieldNode = fieldNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			foreach (var variableResult in analyzeResult.Variables)
			{
				var spanStart = variableResult.Node.SpanStart - startRootSpan;
				var spanLength = variableResult.Node.Span.Length;
				var accessorNode = fieldNode.DescendantNodes()
					.First(o => o.SpanStart == spanStart && o.Span.Length == spanLength);
				var transformedNode = new FieldVariableTransformationResult(variableResult);
				result.TransformedVariables.Add(transformedNode);
				fieldNode = fieldNode.ReplaceNode(accessorNode, accessorNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = fieldNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				fieldNode = fieldNode
					.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async").WithTriviaFrom(nameNode.Identifier)));
			}

			foreach (var fieldVariableResult in result.TransformedVariables.OrderByDescending(o => o.OriginalStartSpan))
			{
				var variableNode = (VariableDeclaratorSyntax) fieldNode.GetAnnotatedNodes(fieldVariableResult.Annotation)
					.First();
				var variableTransformResult = TransformFieldVariable(variableNode, fieldVariableResult, canCopy);
				fieldNode = variableTransformResult.Transformed == null
					? fieldNode.RemoveNode(variableNode, SyntaxRemoveOptions.KeepUnbalancedDirectives)
					: fieldNode.ReplaceNode(variableNode, variableTransformResult.Transformed);
			}

			result.Transformed = fieldNode;
			return result;
		}

		private FieldVariableTransformationResult TransformFieldVariable(
			VariableDeclaratorSyntax variableNode,
			FieldVariableTransformationResult result,
			bool canCopy)
		{
			var variableResult = result.AnalyzationResult;
			var conversion = variableResult.Conversion;
			if (conversion == FieldVariableConversion.Ignore || !canCopy)
			{
				return result;
			}

			var startVariableSpan = variableResult.Node.Span.Start;
			variableNode = variableNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startVariableSpan -= variableNode.SpanStart;

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in variableResult.TypeReferences.Where(o => o.TypeAnalyzationResult.Conversion == TypeConversion.NewType))
			{
				var reference = typeReference.ReferenceLocation;
				var refSpanStart = reference.Location.SourceSpan.Start - startVariableSpan;
				var nameNode = variableNode.GetSimpleName(refSpanStart, reference.Location.SourceSpan.Length, typeReference.IsCref);
				var annotation = Guid.NewGuid().ToString();
				variableNode = variableNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = variableNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				variableNode = variableNode
					.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async").WithTriviaFrom(nameNode.Identifier)));
			}

			result.Transformed = variableNode;
			return result;
		}
	}
}
