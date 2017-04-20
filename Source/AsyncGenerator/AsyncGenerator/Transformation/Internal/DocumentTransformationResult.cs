using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class DocumentTransformationResult : TransformationResult<CompilationUnitSyntax>, IDocumentTransformationResult
	{
		public DocumentTransformationResult(CompilationUnitSyntax node) : base(node)
		{
		}

		public CompilationUnitSyntax OriginalModifiedNode { get; set; }

		#region IDocumentTransformationResult

		CompilationUnitSyntax IDocumentTransformationResult.Original => Node;

		CompilationUnitSyntax IDocumentTransformationResult.OriginalModified => OriginalModifiedNode;

		CompilationUnitSyntax IDocumentTransformationResult.Transformed => TransformedNode;

		#endregion
	}
}
