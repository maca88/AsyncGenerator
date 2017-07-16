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
	internal class TypeTransformationResult : TransformationResult, ITypeTransformationResult, ITypeTransformationMetadata
	{
		public TypeTransformationResult(ITypeAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public ITypeAnalyzationResult AnalyzationResult { get; }

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();

		public Dictionary<string, SyntaxToken> TransformedTokens { get; } = new Dictionary<string, SyntaxToken>();

		public List<MethodTransformationResult> TransformedMethods { get; } = new List<MethodTransformationResult>();

		public List<FunctionTransformationResult> TransformedSpecialMethods { get; } = new List<FunctionTransformationResult>();

		public List<PropertyTransformationResult> TransformedProperties { get; } = new List<PropertyTransformationResult>();

		public TypeTransformationResult Partial { get; set; }

		public IImmutableSet<string> MemberNames { get; set; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		#region ITypeTransformationResult

		private IReadOnlyList<IMethodTransformationResult> _cachedTransformedMethods;
		IReadOnlyList<IMethodTransformationResult> ITypeTransformationResult.TransformedMethods =>
			_cachedTransformedMethods ?? (_cachedTransformedMethods = TransformedMethods.Where(o => o.Transformed != null).ToImmutableList());

		#endregion

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Transformed != null)
			{
				yield return Transformed;
			}
			if (Partial != null)
			{
				yield return Partial.Transformed;
			}
		}
	}
}
