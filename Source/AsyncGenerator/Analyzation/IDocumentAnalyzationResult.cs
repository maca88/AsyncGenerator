using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IDocumentAnalyzationResult
	{
		Document Document { get; }

		CompilationUnitSyntax Node { get; }

		IReadOnlyList<INamespaceAnalyzationResult> Namespaces { get; }

		INamespaceAnalyzationResult GlobalNamespace { get; }

		IEnumerable<ITypeAnalyzationResult> GetAllTypes();
	}
}
