using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation
{
	public interface IDocumentTransformationResult
	{
		/// <summary>
		/// The original document prior the transformation
		/// </summary>
		CompilationUnitSyntax Original { get; }

		/// <summary>
		/// The modified original document that will be set only if the there were some changes needed to be done (eg. added partial keyword)
		/// </summary>
		CompilationUnitSyntax OriginalModified { get; }

		/// <summary>
		/// The transformed document
		/// </summary>
		CompilationUnitSyntax Transformed { get; }

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
