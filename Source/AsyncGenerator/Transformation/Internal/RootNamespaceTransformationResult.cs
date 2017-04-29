using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Transformation.Internal
{
	internal class RootNamespaceTransformationResult : NamespaceTransformationResult
	{
		public RootNamespaceTransformationResult(INamespaceAnalyzationResult analyzationResult) : base(analyzationResult)
		{
		}

		public List<NamespaceTransformationResult> DescendantTransformedNamespaces { get; } = new List<NamespaceTransformationResult>();

		public IEnumerable<NamespaceTransformationResult> GetSelfAndDescendantTransformedNamespaces()
		{
			yield return this;
			foreach (var transformType in DescendantTransformedNamespaces)
			{
				yield return transformType;
			}
		}
	}
}
