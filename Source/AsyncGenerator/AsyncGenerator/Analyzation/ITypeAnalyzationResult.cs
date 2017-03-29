using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	public interface ITypeAnalyzationResult
	{
		INamedTypeSymbol Symbol { get; }

		TypeDeclarationSyntax Node { get; }

		TypeConversion Conversion { get; }

		/// <summary>
		/// References of types that are used inside this type
		/// </summary>
		IReadOnlyList<ReferenceLocation> TypeReferences { get; }

		/// <summary>
		/// References of itself
		/// </summary>
		IReadOnlyList<ReferenceLocation> SelfReferences { get; }

		IReadOnlyList<ITypeAnalyzationResult> NestedTypes { get; }

		IReadOnlyList<IMethodAnalyzationResult> Methods { get; }
	}
}
