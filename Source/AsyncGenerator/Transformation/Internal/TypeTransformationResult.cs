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
	/// <summary>
	/// Holds the current information about the type that is under transformation process
	/// </summary>
	public interface ITypeTransformationMetadata
	{
		ITypeAnalyzationResult AnalyzationResult { get; }

		IImmutableSet<string> MemberNames { get; }

		SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		SyntaxTrivia EndOfLineTrivia { get; set; }

		SyntaxTrivia IndentTrivia { get; set; }
	}

	internal class TypeTransformationResult : TransformationResult, ITypeTransformationResult, ITypeTransformationMetadata
	{
		public TypeTransformationResult(ITypeAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public ITypeAnalyzationResult AnalyzationResult { get; }

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();

		public List<MethodTransformationResult> TransformedMethods { get; } = new List<MethodTransformationResult>();

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
	}
}
