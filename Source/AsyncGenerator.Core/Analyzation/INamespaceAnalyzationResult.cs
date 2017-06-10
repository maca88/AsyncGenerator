using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface INamespaceAnalyzationResult : IMemberAnalyzationResult
	{
		NamespaceDeclarationSyntax Node { get; }

		INamespaceSymbol Symbol { get; }

		NamespaceConversion Conversion { get; }

		/// <summary>
		/// References of types that are used inside this namespace (alias to a type with a using statement)
		/// </summary>
		IReadOnlyList<ITypeReferenceAnalyzationResult> TypeReferences { get; }

		IReadOnlyList<ITypeAnalyzationResult> Types { get; }

		IReadOnlyList<INamespaceAnalyzationResult> NestedNamespaces { get; }

		/// <summary>
		/// Check if the current merged namespace or any of the merged parent namespaces have a type with the given name
		/// </summary>
		bool ContainsType(string name);

		/// <summary>
		/// Check whether the given namespace is included in the current namespace or one of its parents. 
		/// A namespace is included when imported using the using keyword or is the parent of the current one.
		/// </summary>
		bool IsIncluded(string fullNamespace);
	}
}
