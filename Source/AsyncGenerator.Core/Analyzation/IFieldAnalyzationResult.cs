using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IFieldAnalyzationResult :  IMemberAnalyzationResult
	{
		BaseFieldDeclarationSyntax Node { get; }

		/// <summary>
		/// Returns variables of the field.
		/// </summary>
		IReadOnlyList<IFieldVariableDeclaratorResult> Variables { get; }

		/// <summary>
		/// References of types that are used inside this type
		/// </summary>
		IReadOnlyList<ITypeReferenceAnalyzationResult> TypeReferences { get; }
	}
}
