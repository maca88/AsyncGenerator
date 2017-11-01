using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal class NamespaceTransformationResult : TransformationResult, INamespaceTransformationResult, INamespaceTransformationMetadata
	{
		public NamespaceTransformationResult(INamespaceAnalyzationResult analyzationResult) : base(analyzationResult.Node)
		{
			AnalyzationResult = analyzationResult;
		}

		public INamespaceAnalyzationResult AnalyzationResult { get; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		public bool TaskConflict { get; set; }

		public bool UsingSystem { get; set; }

		public HashSet<string> AdditionalUsings { get; } = new HashSet<string>();

		public void AddUsing(string ns)
		{
			AdditionalUsings.Add(ns);
		}

		/// <summary>
		/// Used for check if we have to add the System.Threading namespace because of <see cref="CancellationToken"/>
		/// </summary>
		public bool ThreadingUsingRequired { get; set; }

		public List<RootTypeTransformationResult> TransformedTypes { get; } = new List<RootTypeTransformationResult>();

		#region INamespaceTransformationResult

		private IReadOnlyList<ITypeTransformationResult> _cachedTransformedTypes;
		IReadOnlyList<ITypeTransformationResult> INamespaceTransformationResult.TransformedTypes =>
			_cachedTransformedTypes ?? (_cachedTransformedTypes = TransformedTypes
				.SelectMany(o => o.GetSelfAndDescendantTransformedTypes().Where(t => t.Transformed != null)).ToImmutableArray());

		#endregion

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}
	}
}
