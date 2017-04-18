using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation
{
	public class DocumentTransformationResult : TransformationResult<CompilationUnitSyntax>
	{
		public DocumentTransformationResult(CompilationUnitSyntax node) : base(node)
		{
		}

		public CompilationUnitSyntax OriginalModifiedNode { get; set; }
	}

	public class TypeTransformationResult : TransformationResult<TypeDeclarationSyntax>
	{
		public TypeTransformationResult(TypeDeclarationSyntax node) : base(node)
		{
		}

		public TypeDeclarationSyntax OriginalModifiedNode { get; set; }
	}

	public class MethodTransformationResult : TransformationResult
	{
		public MethodTransformationResult(IMethodAnalyzationResult methodResult) : base(methodResult.Node)
		{
			MethodAnalyzationResult = methodResult;
		}

		public IMethodAnalyzationResult MethodAnalyzationResult { get; }

		public FieldDeclarationSyntax AsyncLockField { get; set; }
	}

	public class TransformationResult : TransformationResult<SyntaxNode>
	{
		public TransformationResult(SyntaxNode node) : base(node)
		{
		}
	}

	public class TransformationResult<T> : AnnotatedNode<T> where T : SyntaxNode
	{
		public TransformationResult(T node) : base(node)
		{
		}

		public T TransformedNode { get; set; }
	}

	public class AnnotatedNode<T> where T : SyntaxNode
	{
		public AnnotatedNode(T node)
		{
			Node = node;
		}

		public T Node { get; }

		public string Annotation { get; } = Guid.NewGuid().ToString();
	}

	

	

	public class TypeTransformationMetadata
	{
		public string Annotation { get; } = Guid.NewGuid().ToString();

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();

		public List<MethodTransformationResult> TransformedMethods { get; } = new List<MethodTransformationResult>();

		public HashSet<string> ReservedFieldNames { get; set; }
	}


	internal class ProjectTransformer
	{
		private readonly ProjectTransformConfiguration _configuration;

		public ProjectTransformer(ProjectTransformConfiguration configuration)
		{
			_configuration = configuration;
		}

		public void Transform(IProjectAnalyzationResult analyzationResult)
		{
			foreach (var document in analyzationResult.Documents)
			{
				TransformDocument(document);
			}
		}

		private DocumentTransformationResult TransformDocument(IDocumentAnalyzationResult documentResult)
		{
			var rootNode = documentResult.Node;
			var result = new DocumentTransformationResult(rootNode);
			var rewrittenNodes = new List<TransformationResult>();
			var namespaceNodes = new List<MemberDeclarationSyntax>();
			var hasTaskUsing = rootNode.Usings.Any(o => o.Name.ToString() == "System.Threading.Tasks");

			foreach (var namespaceResult in documentResult.Namespaces.OrderBy(o => o.Node.SpanStart))
			{
				var namespaceNode = namespaceResult.Node;
				var typeNodes = new List<MemberDeclarationSyntax>();
				foreach (var typeResult in namespaceResult.Types.Where(o => o.Conversion != TypeConversion.Ignore).OrderBy(o => o.Node.SpanStart))
				{
					var transformResult = TransformType(typeResult);
					if (transformResult.TransformedNode == null)
					{
						continue;
					}
					typeNodes.Add(transformResult.TransformedNode);

					// We need to update the original file if it was modified
					if (transformResult.OriginalModifiedNode != null)
					{
						var typeSpanStart = typeResult.Node.SpanStart;
						var typeSpanLength = typeResult.Node.Span.Length;
						var typeNode = rootNode.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
							.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
						var rewritenNode = new TransformationResult(typeNode)
						{
							TransformedNode = transformResult.OriginalModifiedNode
						};
						rootNode = rootNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(rewritenNode.Annotation)));
						rewrittenNodes.Add(rewritenNode);
					}

					// TODO: missing members
					//if (typeInfo.TypeTransformation == TypeTransformation.NewType && typeInfo.HasMissingMembers)
					//{
					//	transformResult = TransformType(typeInfo, true);
					//	if (transformResult.Node == null)
					//	{
					//		continue;
					//	}
					//	typeNodes.Add(transformResult.Node);
					//}
				}
				if (typeNodes.Any())
				{
					//TODO: check if Task is conflicted inside namespace
					if (!hasTaskUsing && namespaceNode.Usings.All(o => o.Name.ToString() != "System.Threading.Tasks"))
					{
						namespaceNode = namespaceNode.AddUsing("System.Threading.Tasks");
					}
					// TODO: add locking namespaces

					namespaceNodes.Add(namespaceNode
						.WithMembers(List(typeNodes)));
				}
			}
			if (!namespaceNodes.Any())
			{
				return result;
			}
			// Update the original node
			var origRootNode = rootNode;
			foreach (var rewrittenNode in rewrittenNodes)
			{
				origRootNode = rootNode.ReplaceNode(rootNode.GetAnnotatedNodes(rewrittenNode.Annotation).First(), rewrittenNode.TransformedNode);
			}
			if (rootNode != origRootNode)
			{
				result.OriginalModifiedNode = origRootNode;
			}

			// Create the new node
			rootNode = rootNode
					.WithMembers(List(namespaceNodes));
			// Add auto-generated comment
			var token = rootNode.DescendantTokens().First();
			rootNode = rootNode.ReplaceToken(token, token.AddAutoGeneratedTrivia());

			result.TransformedNode = rootNode;
			return result;
		}

		// TODO: Missing members
		private TypeTransformationResult TransformType(ITypeAnalyzationResult rootTypeResult)
		{
			var rootTypeNode = rootTypeResult.Node;
			var startRootTypeSpan = rootTypeNode.SpanStart;
			var typeResultMetadatas = new Dictionary<ITypeAnalyzationResult, TypeTransformationMetadata>();
			var rootMetadata = new TypeTransformationMetadata
			{
				ReservedFieldNames = new HashSet<string>(rootTypeResult.Symbol.MemberNames)
			};
			// We do this here because we want that the root node has span start equal to 0
			rootTypeNode = rootTypeNode.WithAdditionalAnnotations(new SyntaxAnnotation(rootMetadata.Annotation));
			startRootTypeSpan -= rootTypeNode.SpanStart;

			// Before any modification we need to annotate nodes that will be transformed in order to find them later on. 
			// We cannot rely on spans as they changes each time the node is modified.
			foreach (var typeResult in rootTypeResult.GetSelfAndDescendantsTypes(o => o.Conversion != TypeConversion.Ignore))
			{
				var typeSpanStart = typeResult.Node.SpanStart - startRootTypeSpan;
				var typeSpanLength = typeResult.Node.Span.Length;
				var typeNode = rootTypeNode.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
					.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
				TypeTransformationMetadata metadata;
				if (typeNode == rootTypeNode)
				{
					metadata = rootMetadata;
				}
				else
				{
					metadata = new TypeTransformationMetadata
					{
						ReservedFieldNames = new HashSet<string>(typeResult.Symbol.MemberNames)
					};
					rootTypeNode = rootTypeNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(metadata.Annotation)));
				}

				// TypeReferences can be changes only if we create a new type
				if (rootTypeResult.Conversion == TypeConversion.NewType)
				{
					foreach (var typeReference in typeResult.TypeReferences)
					{
						var reference = typeReference.ReferenceLocation;
						var refSpanStart = reference.Location.SourceSpan.Start - startRootTypeSpan;
						var refSpanLength = reference.Location.SourceSpan.Length;
						if (refSpanStart < 0)
						{
							// TODO: cref
							//var startSpan = reference.Location.SourceSpan.Start - rootTypeInfo.Node.GetLeadingTrivia().Span.Start;
							//var crefNode = leadingTrivia.First(o => o.SpanStart == startSpan && o.Span.Length == refSpanLength);
							continue;
						}

						var nameNode = rootTypeNode.GetSimpleName(refSpanStart, refSpanLength);
						var transformedNode = new TransformationResult(nameNode)
						{
							TransformedNode = nameNode.WithIdentifier(Identifier(nameNode.Identifier.ValueText + "Async"))
						};
						metadata.TransformedNodes.Add(transformedNode);
						rootTypeNode = rootTypeNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));
						
					}
				}

				// Annotate and save the transformed methods
				foreach (var methodResult in typeResult.Methods)
				{
					var methodSpanStart = methodResult.Node.SpanStart - startRootTypeSpan;
					var methodSpanLength = methodResult.Node.Span.Length;
					var methodNode = rootTypeNode.DescendantNodes()
											 .OfType<MethodDeclarationSyntax>()
											 .First(o => o.SpanStart == methodSpanStart && o.Span.Length == methodSpanLength);
					var transformedNode = TransformMethod(metadata, methodResult);
					metadata.TransformedMethods.Add(transformedNode);
					rootTypeNode = rootTypeNode.ReplaceNode(methodNode, methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(transformedNode.Annotation)));

				}
				typeResultMetadatas.Add(typeResult, metadata);

			}
			// Save the orignal node that was only annotated
			var result = new TypeTransformationResult(rootTypeNode);

			// Now we can start transforming the type. Start from the bottom in order to preserve replaced nested types
			foreach (var typeResult in rootTypeResult.GetSelfAndDescendantsTypes(o => o.Conversion != TypeConversion.Ignore)
				.OrderByDescending(o => o.Node.SpanStart))
			{
				var metadata = typeResultMetadatas[typeResult];
				// Add partial keyword on the original node if not present
				if (typeResult.Conversion == TypeConversion.Partial && !typeResult.IsPartial)
				{
					var typeNode = rootTypeNode.GetAnnotatedNodes(metadata.Annotation).OfType<TypeDeclarationSyntax>().First();
					result.OriginalModifiedNode = result.Node.ReplaceNode(typeNode, typeNode.AddPartial());
				}
				// If the root type has to be a new type then all nested types have to be new types
				if (typeResult.Conversion == TypeConversion.NewType)
				{
					// Replace all rewritten nodes
					foreach (var rewNode in metadata.TransformedNodes)
					{
						var node = rootTypeNode.GetAnnotatedNodes(rewNode.Annotation).First();
						if (rewNode.TransformedNode == null)
						{
							//TODO: fix regions
							rootTypeNode = rootTypeNode.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
						}
						else
						{
							rootTypeNode = rootTypeNode.ReplaceNode(node, rewNode.TransformedNode);
						}
					}
				}
				else if (typeResult.Conversion == TypeConversion.Partial)
				{
					var typeNode = rootTypeNode.GetAnnotatedNodes(metadata.Annotation).OfType<TypeDeclarationSyntax>().First();
					var newNodes =  metadata.TransformedNodes
							.Union(metadata.TransformedMethods)
							.Where(o => o.TransformedNode != null)
							.OrderBy(o => o.Node.SpanStart)
							.Select(o => o.TransformedNode)
						.Union(typeNode.DescendantNodes().OfType<TypeDeclarationSyntax>())
						.ToList();
					if (!newNodes.Any())
					{
						//TODO: fix regions
						rootTypeNode = rootTypeNode.RemoveNode(typeNode, SyntaxRemoveOptions.KeepNoTrivia);
					}
					else
					{
						// We need to remove the attributes as they cannot be defined in both partial classes
						var newTypeNode = typeNode.AddPartial().WithoutAttributes();
						// Add fields for async lock if any. We need a lock field for each synchronized method
						foreach (var methodTransform in metadata.TransformedMethods.Where(o => o.AsyncLockField != null).OrderBy(o => o.Node.SpanStart))
						{
							newNodes.Insert(0, methodTransform.AsyncLockField);
						}
						newTypeNode = newTypeNode.WithMembers(List(newNodes));

						//TODO: fix regions
						rootTypeNode = rootTypeNode.ReplaceNode(typeNode, newTypeNode/*.RemoveLeadingDirectives()*/);
					}
				}
			}

			return result;
		}

		private MethodTransformationResult TransformMethod(TypeTransformationMetadata typeMetadata, IMethodAnalyzationResult methodResult)
		{
			var result = new MethodTransformationResult(methodResult);
			if (methodResult.Conversion == MethodConversion.Ignore)
			{
				return result;
			}
			var methodNode = methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();
			if (methodBodyNode == null)
			{
				result.TransformedNode = methodNode.ReturnAsTask(methodResult.Symbol)
					.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;
			// TODO: get leading trivia for the method

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in methodResult.TypeReferences)
			{
				var reference = typeReference.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			var referenceAnnotations = new Dictionary<string, IFunctionReferenceAnalyzationResult>();
			foreach (var referenceResult in methodResult.CrefMethodReferences
				.Union(methodResult.MethodReferences)
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				referenceAnnotations.Add(annotation, referenceResult);
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				methodNode = methodNode
							.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
			}

			foreach (var pair in referenceAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(pair.Key).OfType<SimpleNameSyntax>().First();
				var referenceResult = pair.Value;
				var functionReferenceResult = referenceResult as IInvokeFunctionReferenceAnalyzationResult;
				// If we have a cref or a non awaitable invocation just change the name to the async counterpart
				if (functionReferenceResult == null || !functionReferenceResult.AwaitInvocation)
				{
					
				}

			}

			return result;
		}

	}
}
