using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IAnalyzationResult
	{
		/// <summary>
		/// Get the member node
		/// </summary>
		/// <returns></returns>
		SyntaxNode GetNode();
	}
}
