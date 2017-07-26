using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal abstract class AnnotatedNode<T>: AnnotatedNode where T : SyntaxNode
	{
		protected AnnotatedNode(T originalNode)
		{
			OriginalNode = originalNode;
		}

		public T OriginalNode { get; }

		
	}

	internal abstract class AnnotatedNode
	{
		public string Annotation { get; } = Guid.NewGuid().ToString();

		public abstract int OriginalStartSpan { get; }
	}
}
