using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IMethodAnalyzationResult : IFunctionAnalyzationResult
	{
		MethodConversion Conversion { get; }

		MethodDeclarationSyntax Node { get; }

		bool IsAsync { get; }

		/// <summary>
		/// Methods that invokes this method
		/// </summary>
		IReadOnlyList<IFunctionAnalyzationResult> InvokedBy { get; }

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		IMethodSymbol BaseOverriddenMethod { get; }

		/// <summary>
		/// Reference to the async counterpart for this method
		/// </summary>
		IMethodSymbol AsyncCounterpartSymbol { get; }

		/// <summary>
		/// Anonymous functions that are declared inside the method
		/// </summary>
		IReadOnlyList<IAnonymousFunctionAnalyzationResult> AnonymousFunctions { get; }
	}
}
