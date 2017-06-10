using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class DocumentTransformationResult : TransformationResult<CompilationUnitSyntax>, IDocumentTransformationResult
	{
		public DocumentTransformationResult(IDocumentAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public IDocumentAnalyzationResult AnalyzationResult { get; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public List<RootNamespaceTransformationResult> TransformedNamespaces { get; } = new List<RootNamespaceTransformationResult>();

		public List<RootTypeTransformationResult> TransformedTypes { get; } = new List<RootTypeTransformationResult>();

		#region IDocumentTransformationResult

		private IReadOnlyList<INamespaceTransformationResult> _cachedTransformedNamespaces;
		IReadOnlyList<INamespaceTransformationResult> IDocumentTransformationResult.TransformedNamespaces =>
			_cachedTransformedNamespaces ?? (_cachedTransformedNamespaces = TransformedNamespaces
				.SelectMany(o => o.GetSelfAndDescendantTransformedNamespaces().Where(n => n.Transformed != null)).ToImmutableArray());

		private IReadOnlyList<ITypeTransformationResult> _cachedTransformedTypes;
		IReadOnlyList<ITypeTransformationResult> IDocumentTransformationResult.TransformedTypes =>
			_cachedTransformedTypes ?? (_cachedTransformedTypes = TransformedTypes
				.SelectMany(o => o.GetSelfAndDescendantTransformedTypes().Where(n => n.Transformed != null)).ToImmutableArray());

		#endregion
	}
}
