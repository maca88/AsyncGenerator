using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IDocumentAnalyzationResult : IAnalyzationResult
	{
		Document Document { get; }

		CompilationUnitSyntax Node { get; }

		IReadOnlyList<INamespaceAnalyzationResult> Namespaces { get; }

		// TODO: global namespace cannot have NestedNamespaces
		INamespaceAnalyzationResult GlobalNamespace { get; }

		IEnumerable<ITypeAnalyzationResult> GetAllTypes();
	}
}
