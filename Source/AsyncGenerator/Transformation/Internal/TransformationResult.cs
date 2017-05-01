using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
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

		public T Transformed { get; set; }

		public T OriginalModified { get; set; }

		public int OriginalStartSpan => OriginalNode.SpanStart;

		public virtual IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Transformed != null)
			{
				yield return Transformed;
			}
		}
	}
}
