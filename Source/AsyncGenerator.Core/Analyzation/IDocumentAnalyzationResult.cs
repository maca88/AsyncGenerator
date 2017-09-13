using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IDocumentAnalyzationResult
	{
		Document Document { get; }

		CompilationUnitSyntax Node { get; }

		INamespaceAnalyzationResult GlobalNamespace { get; }

		IEnumerable<ITypeAnalyzationResult> AllTypes { get; }

		IEnumerable<INamespaceAnalyzationResult> AllNamespaces { get; }
	}
}
