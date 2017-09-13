using System.Collections.Generic;
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

		/// <summary>
		/// References of objects that are referenced inside this object
		/// </summary>
		IReadOnlyList<IReferenceAnalyzationResult> References { get; }

		/// <summary>
		/// References of this objects inside other objects
		/// </summary>
		IReadOnlyList<IReferenceAnalyzationResult> SelfReferences { get; }

		/// <summary>
		/// Objects that references this object
		/// </summary>
		IEnumerable<IAnalyzationResult> ReferencedBy { get; }
	}
}
