using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Transformation
{
	public interface IDocumentTransformationResult : ITransformationResult
	{
		/// <summary>
		/// The modified original document that will be set only if the there were some changes needed to be done (eg. added partial keyword)
		/// </summary>
		CompilationUnitSyntax OriginalModified { get; }

		/// <summary>
		/// The transformed document
		/// </summary>
		CompilationUnitSyntax Transformed { get; }

		/// <summary>
		/// End of line trivia
		/// </summary>
		SyntaxTrivia EndOfLineTrivia { get; set; }

		/// <summary>
		/// The document analyzation result
		/// </summary>
		IDocumentAnalyzationResult AnalyzationResult { get; }

		/// <summary>
		/// All transformed namespaces inside this document
		/// </summary>
		IReadOnlyList<INamespaceTransformationResult> TransformedNamespaces { get; }

		/// <summary>
		/// All transformed global types inside this document
		/// </summary>
		IReadOnlyList<ITypeTransformationResult> TransformedTypes { get; }
	}
}
