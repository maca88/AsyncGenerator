using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Core.Transformation
{
	public interface IFunctionReferenceTransformationResult : ITransformationResult
	{
		IFunctionReferenceAnalyzationResult AnalyzationResult { get; }
	}
}
