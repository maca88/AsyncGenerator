using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	public interface INamespaceAnalyzationResult
	{
		NamespaceDeclarationSyntax Node { get; }

		INamespaceSymbol Symbol { get; }

		/// <summary>
		/// References of types that are used inside this namespace (alias to a type with a using statement)
		/// </summary>
		IReadOnlyList<ITypeReferenceAnalyzationResult> TypeReferences { get; }

		IReadOnlyList<ITypeAnalyzationResult> Types { get; }
	}
}
