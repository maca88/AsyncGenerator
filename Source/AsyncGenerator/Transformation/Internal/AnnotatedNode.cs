using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal class AnnotatedNode<T> where T : SyntaxNode
	{
		public AnnotatedNode(T originalNode)
		{
			OriginalNode = originalNode;
		}

		public T OriginalNode { get; }

		public string Annotation { get; } = Guid.NewGuid().ToString();
	}
}
