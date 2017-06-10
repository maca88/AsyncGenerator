namespace AsyncGenerator.Core.Analyzation
{
	public interface IMemberAnalyzationResult : IAnalyzationResult
	{
		/// <summary>
		/// Get the next member in the hierarchy. If the member does not contain a next sibling the parent is returned
		/// </summary>
		IMemberAnalyzationResult GetNext();

		/// <summary>
		/// Get the previous member in the hierarchy. If the member does not contain a previous sibling the parent is returned
		/// </summary>
		IMemberAnalyzationResult GetPrevious();

		/// <summary>
		/// Check whether the given analyzation result is the parent of the current one 
		/// </summary>
		/// <param name="analyzationResult">The analyzation result to check</param>
		/// <returns></returns>
		bool IsParent(IAnalyzationResult analyzationResult);
	}
}
