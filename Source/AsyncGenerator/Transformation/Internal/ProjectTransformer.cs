using System;
using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class RootTypeTransformationResult : TypeTransformationResult
	{
		public RootTypeTransformationResult(ITypeAnalyzationResult typeAnalyzationResult) : base(typeAnalyzationResult)
		{
		}

		public List<TypeTransformationResult> DescendantTransformTypeResults { get; } = new List<TypeTransformationResult>();

		public IEnumerable<TypeTransformationResult> GetSelfAndDescendantTypes()
		{
			yield return this;
			foreach (var transformType in DescendantTransformTypeResults)
			{
				yield return transformType;
			}
		}
	}

	internal class TypeTransformationResult : TransformationResult
	{
		public TypeTransformationResult(ITypeAnalyzationResult typeAnalyzationResult) : base(typeAnalyzationResult.Node)
		{
			TypeAnalyzationResult = typeAnalyzationResult;
		}

		public ITypeAnalyzationResult TypeAnalyzationResult { get; }

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();

		public List<MethodTransformationResult> TransformedMethods { get; } = new List<MethodTransformationResult>();

		public HashSet<string> ReservedFieldNames { get; set; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

	}

	internal class MethodTransformationResult : TransformationResult
	{
		public MethodTransformationResult(IMethodAnalyzationResult methodResult) : base(methodResult.Node)
		{
			MethodAnalyzationResult = methodResult;
		}

		public IMethodAnalyzationResult MethodAnalyzationResult { get; }

		public MethodDeclarationSyntax TailMethodNode { get; set; }

		public FieldDeclarationSyntax AsyncLockField { get; set; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		public string TaskReturnedAnnotation { get; set; } = "TaskReturned";

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			yield return TransformedNode;
			if (TailMethodNode != null)
			{
				yield return TailMethodNode;
			}
		}
	}

	internal class TransformationResult : TransformationResult<SyntaxNode>
	{
		public TransformationResult(SyntaxNode originalNode) : base(originalNode)
		{
		}
	}

	internal class TransformationResult<T> : AnnotatedNode<T> where T : SyntaxNode
	{
		public TransformationResult(T originalNode) : base(originalNode)
		{
		}

		public T TransformedNode { get; set; }

		public T OriginalModifiedNode { get; set; }

		public virtual IEnumerable<T> GetTransformedNodes()
		{
			yield return TransformedNode;
		}
	}

	internal class AnnotatedNode<T> where T : SyntaxNode
	{
		public AnnotatedNode(T originalNode)
		{
			OriginalNode = originalNode;
		}

		public T OriginalNode { get; }

		public string Annotation { get; } = Guid.NewGuid().ToString();
	}

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
				// TODO: option to modify the document
			}
			return result;
		}
	}
}
