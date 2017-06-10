namespace AsyncGenerator.Core.Analyzation
{
	public interface IChildFunctionAnalyzationResult : IFunctionAnalyzationResult
	{
		IFunctionAnalyzationResult ParentFunction { get; }
	}
}
