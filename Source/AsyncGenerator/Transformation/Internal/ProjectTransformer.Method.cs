using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private MethodTransformationResult TransformMethod(MethodDeclarationSyntax methodNode, bool canCopy, MethodTransformationResult result, ITypeTransformationMetadata typeMetadata, 
			INamespaceTransformationMetadata namespaceMetadata)
		{
			//var result = new MethodTransformationResult(methodResult);
			var methodResult = result.AnalyzationResult;
			var methodConversion = methodResult.Conversion;
			if (!canCopy)
			{
				methodConversion &= ~MethodConversion.Copy;
			}
			//var methodNode = customNode ?? methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();

			// Calculate whitespace method trivias
			result.EndOfLineTrivia = methodNode.GetEndOfLine();
			result.LeadingWhitespaceTrivia = methodNode.GetLeadingWhitespace();
			result.IndentTrivia = methodNode.GetIndent(result.LeadingWhitespaceTrivia, typeMetadata.LeadingWhitespaceTrivia);
			result.BodyLeadingWhitespaceTrivia = Whitespace(result.LeadingWhitespaceTrivia.ToFullString() + result.IndentTrivia.ToFullString());

			if (methodConversion == MethodConversion.Ignore)
			{
				return result;
			}

			if (methodBodyNode == null)
			{
				if (methodConversion.HasFlag(MethodConversion.ToAsync))
				{
					result.Transformed = methodNode;
					if (methodConversion.HasFlag(MethodConversion.Copy))
					{
						result.AddMethod(methodResult.Node);
					}
					return result;
				}
				if (methodConversion.HasFlag(MethodConversion.Copy))
				{
					result.Transformed = methodResult.Node;
				}
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in methodResult.TypeReferences.Where(o => o.TypeAnalyzationResult.Conversion == TypeConversion.NewType))
			{
				var reference = typeReference.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			// For copied methods we need just to replace type references
			if (methodConversion.HasFlag(MethodConversion.Copy))
			{
				var copiedMethod = methodNode;
				// Modify references
				foreach (var refAnnotation in typeReferencesAnnotations)
				{
					var nameNode = copiedMethod.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
					copiedMethod = copiedMethod
						.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async").WithTriviaFrom(nameNode.Identifier)));
				}
				if (!methodConversion.HasFlag(MethodConversion.ToAsync))
				{
					result.Transformed = copiedMethod;
					return result;
				}
				result.AddMethod(copiedMethod.WithoutAnnotations(result.Annotation));
			}

			foreach (var childFunction in methodResult.ChildFunctions.Where(o => o.Conversion != MethodConversion.Ignore))
			{
				var functionNode = childFunction.GetNode();
				var functionKind = functionNode.Kind();
				var typeSpanStart = functionNode.SpanStart - startMethodSpan;
				var typeSpanLength = functionNode.Span.Length;
				var funcNode = methodNode.DescendantNodesAndSelf()
					.First(o => o.IsKind(functionKind) && o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				var transformFuncResult = TransformFunction(childFunction, result, typeMetadata, namespaceMetadata);
				result.TransformedFunctions.Add(transformFuncResult);
				methodNode = methodNode.ReplaceNode(funcNode, funcNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformFuncResult.Annotation)));
			}

			foreach (var referenceResult in methodResult.FunctionReferences
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var transfromReference = new FunctionReferenceTransformationResult(referenceResult);
				var isCref = referenceResult.IsCref;
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length, isCref);
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transfromReference.Annotation)));
				result.TransformedFunctionReferences.Add(transfromReference);

				if (isCref || referenceResult.IsNameOf || !methodResult.OmitAsync)
				{
					continue;
				}
				// We need to annotate the reference node (InvocationExpression, IdentifierName) in order to know if we need to wrap the node in a Task.FromResult
				var refNode = referenceResult.ReferenceNode;
				var bodyReference = (IBodyFunctionReferenceAnalyzationResult) referenceResult;
				if (bodyReference.UseAsReturnValue || refNode.IsReturned())
				{
					startSpan = refNode.SpanStart - startMethodSpan;
					var referenceNode = methodNode.DescendantNodes().First(o => o.SpanStart == startSpan && o.Span.Length == refNode.Span.Length);
					methodNode = methodNode.ReplaceNode(referenceNode, referenceNode.WithAdditionalAnnotations(new SyntaxAnnotation(Annotations.TaskReturned)));
				}
			}
			// Before modifying, fixup method body formatting in order to prevent weird formatting when adding additinal code
			methodNode = FixupBodyFormatting(methodNode, result);

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				methodNode = methodNode
							.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async").WithTriviaFrom(nameNode.Identifier)));
			}

			foreach (var transformFunction in result.TransformedFunctions)
			{
				var funcNode = methodNode.GetAnnotatedNodes(transformFunction.Annotation).First();
				methodNode = methodNode
					.ReplaceNode(funcNode, transformFunction.Transformed);
			}

			// We have to order by OriginalStartSpan in order to have consistent formatting when adding awaits
			foreach (var transfromReference in result.TransformedFunctionReferences.OrderByDescending(o => o.OriginalStartSpan))
			{
				methodNode = TransformFunctionReference(methodNode, methodResult, transfromReference, typeMetadata, namespaceMetadata);
			}

			result.Transformed = methodNode;

			return result;
		}

		private MethodDeclarationSyntax FixupBodyFormatting(MethodDeclarationSyntax methodNode, IMethodOrAccessorTransformationResult result)
		{
			var methodBody = methodNode.Body;
			if (methodBody == null)
			{
				if (methodNode.ExpressionBody == null)
				{
					return methodNode;
				}
				// Add space after the close paren token
				if (string.IsNullOrEmpty(methodNode.ParameterList.CloseParenToken.TrailingTrivia.ToFullString()))
				{
					methodNode = methodNode.ReplaceToken(methodNode.ParameterList.CloseParenToken,
						methodNode.ParameterList.CloseParenToken.WithTrailingTrivia(TriviaList(Space)));
				}

				return methodNode;
			}
			// Make a diff by using only the whitespaces
			var triviaLengthDiff = GetLeadingWhitespaceDifference(methodBody, methodNode);
			if (triviaLengthDiff > 0)
			{
				// Normalize leading trivia
				methodNode = methodNode.WithBody(methodBody
					.SubtractIndent(string.Join("", methodBody.GetLeadingTrivia()
						.Where(o => o.IsKind(SyntaxKind.WhitespaceTrivia))
						.Select(o => o.ToFullString())).Substring(0, triviaLengthDiff.Value)));
			}

			var eol = result.EndOfLineTrivia.ToFullString();
			// Add end of line for the close paren token if missing
			if (!methodNode.ConstraintClauses.Any() && !methodNode.ParameterList.CloseParenToken.TrailingTrivia.ToFullString().Contains(eol))
			{
				methodNode = methodNode.ReplaceToken(methodNode.ParameterList.CloseParenToken,
					methodNode.ParameterList.CloseParenToken.WithTrailingTrivia(result.EndOfLineTrivia));
				methodNode = methodNode.ReplaceToken(methodNode.Body.OpenBraceToken,
					methodNode.Body.OpenBraceToken.WithLeadingTrivia(result.LeadingWhitespaceTrivia));
			}
			else if (methodNode.ConstraintClauses.Any() && !methodNode.ConstraintClauses.Last().GetTrailingTrivia().ToFullString().Contains(eol))
			{
				var lastConstraint = methodNode.ConstraintClauses.Last();
				methodNode = methodNode.ReplaceNode(lastConstraint, lastConstraint.WithTrailingTrivia(result.EndOfLineTrivia));
				methodNode = methodNode.ReplaceToken(methodNode.Body.OpenBraceToken,
					methodNode.Body.OpenBraceToken.WithLeadingTrivia(result.LeadingWhitespaceTrivia));
			}
			methodBody = methodNode.Body;
			var getLineSpan = methodBody.GetLocation().GetLineSpan().Span;
			// Add end of line tokens for open brace and statements when the whole block is written in one line (eg. { DoSomething(); })
			if (getLineSpan.End.Line == getLineSpan.Start.Line)
			{
				methodBody = methodBody.ReplaceToken(methodBody.OpenBraceToken,
					methodBody.OpenBraceToken.WithTrailingTrivia(result.EndOfLineTrivia));
				// We have to fix also the statements leading trivia
				for (var i = 0; i < methodBody.Statements.Count; i++)
				{
					var statement = methodBody.Statements[i];
					methodBody = methodBody.ReplaceNode(statement, statement
						.WithLeadingTrivia(result.BodyLeadingWhitespaceTrivia)
						.WithTrailingTrivia(TriviaList(result.EndOfLineTrivia)));
				}
				methodBody = methodBody.ReplaceToken(methodBody.CloseBraceToken,
					methodBody.CloseBraceToken.WithLeadingTrivia(TriviaList(result.LeadingWhitespaceTrivia)));
				methodNode = methodNode.WithBody(methodBody);
			}
			return methodNode;
		}

		private static int? GetLeadingWhitespaceDifference(SyntaxNode node, SyntaxNode node2)
		{
			var nodeLeadingWhitespace = GetLeadingWhitespace(node);
			var node2LeadingWhitespace = GetLeadingWhitespace(node2);
			// Compare only when both nodes are using the same whitespace character
			return HaveSameWhitespaceCharacter(nodeLeadingWhitespace, node2LeadingWhitespace, '\t') ||
			       HaveSameWhitespaceCharacter(nodeLeadingWhitespace, node2LeadingWhitespace, ' ')
				? nodeLeadingWhitespace.Length - node2LeadingWhitespace.Length
				: (int?) null;
		}

		private static bool HaveSameWhitespaceCharacter(string whitespace, string whitespace2, char indentCharacter)
		{
			return whitespace.All(c => c == indentCharacter) && whitespace2.All(c => c == indentCharacter);
		}

		private static string GetLeadingWhitespace(SyntaxNode node)
		{
			return string.Join("", node.GetLeadingTrivia()
				.Where(o => o.IsKind(SyntaxKind.WhitespaceTrivia))
				.Select(o => o.ToFullString()));
		}
	}
}
