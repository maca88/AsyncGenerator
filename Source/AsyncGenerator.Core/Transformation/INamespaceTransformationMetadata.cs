using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Transformation
{
	/// <summary>
	/// Holds the current information about the namespace that is under transformation process
	/// </summary>
	public interface INamespaceTransformationMetadata
	{
		INamespaceAnalyzationResult AnalyzationResult { get; }

		SyntaxTrivia LeadingWhitespaceTrivia { get; }

		SyntaxTrivia EndOfLineTrivia { get; }

		SyntaxTrivia IndentTrivia { get; }

		// TODO: remove - use AnalyzationResult.ContainsType
		/// <summary>
		/// When true, the namespace contains a type with the name Task which is conflict with the built-in <see cref="Task"/> type
		/// </summary>
		bool TaskConflict { get; }

		//TODO: remove - use AnalyzationResult.IsIncluded
		/// <summary>
		/// When true, the current namespace or one of its parents have a using for System
		/// </summary>
		bool UsingSystem { get; }
	}
}
