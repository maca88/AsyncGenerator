using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractData : IAnalyzationResult
	{
		/// <summary>
		/// References of types that are used inside this data (eg. alias to a type with a using statement, cref reference)
		/// </summary>
		public ConcurrentSet<TypeReferenceData> TypeReferences { get; } = new ConcurrentSet<TypeReferenceData>();

		/// <summary>
		/// Get the syntax node of the function
		/// </summary>
		public abstract SyntaxNode GetNode();
	}
}
