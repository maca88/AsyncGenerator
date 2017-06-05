using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Transformation.Internal
{
	internal class RootTypeTransformationResult : TypeTransformationResult
	{
		public RootTypeTransformationResult(ITypeAnalyzationResult analyzationResult) : base(analyzationResult)
		{
		}

		public List<TypeTransformationResult> DescendantTransformedTypes { get; } = new List<TypeTransformationResult>();

		public IEnumerable<TypeTransformationResult> GetSelfAndDescendantTransformedTypes()
		{
			yield return this;
			foreach (var transformType in DescendantTransformedTypes)
			{
				yield return transformType;
			}
		}
	}
}
