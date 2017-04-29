using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Transformation.Internal
{
	internal class NamespaceTransformationResult : TransformationResult, INamespaceTransformationResult
	{
		public NamespaceTransformationResult(INamespaceAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public INamespaceAnalyzationResult AnalyzationResult { get; }

		//public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public bool ThreadingUsingRequired { get; set; }

		public bool SystemUsingRequired { get; set; }

		public List<RootTypeTransformationResult> TransformedTypes { get; } = new List<RootTypeTransformationResult>();

		#region INamespaceTransformationResult

		private IReadOnlyList<ITypeTransformationResult> _cachedTransformedTypes;
		IReadOnlyList<ITypeTransformationResult> INamespaceTransformationResult.TransformedTypes =>
			_cachedTransformedTypes ?? (_cachedTransformedTypes = TransformedTypes
				.SelectMany(o => o.GetSelfAndDescendantTransformedTypes().Where(t => t.TransformedNode != null)).ToImmutableArray());

		#endregion

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}
	}
}
