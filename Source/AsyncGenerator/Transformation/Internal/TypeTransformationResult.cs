using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal class TypeTransformationResult : TransformationResult, ITypeTransformationResult
	{
		public TypeTransformationResult(ITypeAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public ITypeAnalyzationResult AnalyzationResult { get; }

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();

		public List<MethodTransformationResult> TransformedMethods { get; } = new List<MethodTransformationResult>();

		public HashSet<string> ReservedFieldNames { get; set; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		#region ITypeTransformationResult

		private IReadOnlyList<IMethodTransformationResult> _cachedTransformedMethods;
		IReadOnlyList<IMethodTransformationResult> ITypeTransformationResult.TransformedMethods =>
			_cachedTransformedMethods ?? (_cachedTransformedMethods = TransformedMethods.Where(o => o.TransformedNode != null).ToImmutableList());

		#endregion

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}
	}
}
