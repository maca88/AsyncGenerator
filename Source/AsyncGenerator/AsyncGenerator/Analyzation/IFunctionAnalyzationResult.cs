using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	public interface IFunctionAnalyzationResult
	{
		/// <summary>
		/// Symbol of the function
		/// </summary>
		IMethodSymbol Symbol { get; }

		/// <summary>
		/// Get the syntax node of the function
		/// </summary>
		SyntaxNode GetNode();

		/// <summary>
		/// References of types that are used inside this function
		/// </summary>
		IReadOnlyList<ReferenceLocation> TypeReferences { get; }

		/// <summary>
		/// References to other methods that are invoked inside this function and are candidates to be async
		/// </summary>
		IReadOnlyList<ReferenceLocation> MethodReferences { get; }
	}
}
