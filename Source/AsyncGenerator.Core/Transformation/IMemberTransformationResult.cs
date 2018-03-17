using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	public interface IMemberTransformationResult : ITransformationResult, ITransformationTrivia
	{
		IMemberAnalyzationResult GetAnalyzationResult();
	}
}
