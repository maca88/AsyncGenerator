using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal interface ITransformationResult
	{
		SyntaxNode Transformed { get; }

		SyntaxNode OriginalModified { get; }

		int OriginalStartSpan { get; }

		IEnumerable<SyntaxNode> GetTransformedNodes();

		string Annotation { get; }
	}

	internal class TransformationResult : TransformationResult<SyntaxNode>
	{
		public TransformationResult(SyntaxNode originalNode) : base(originalNode)
		{
		}
	}

	internal class TransformationResult<TOriginal> : TransformationResult<TOriginal, TOriginal> where TOriginal : SyntaxNode
	{
		public TransformationResult(TOriginal originalNode) : base(originalNode)
		{
		}
	}

	internal class TransformationResult<TOriginal, TTransformed> : AnnotatedNode<TOriginal> , ITransformationResult
		where TOriginal : SyntaxNode 
		where TTransformed : SyntaxNode
	{
		public TransformationResult(TOriginal originalNode) : base(originalNode)
		{
		}

		public TTransformed Transformed { get; set; }

		public TOriginal OriginalModified { get; set; }

		public override int OriginalStartSpan => OriginalNode.SpanStart;

		public virtual IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Transformed != null)
			{
				yield return Transformed;
			}
		}

		SyntaxNode ITransformationResult.Transformed => Transformed;

		SyntaxNode ITransformationResult.OriginalModified => OriginalModified;
	}
}
