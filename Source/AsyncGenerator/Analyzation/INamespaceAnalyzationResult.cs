using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
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
	}
}
