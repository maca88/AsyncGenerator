using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation
{
	public interface IMethodAnalyzationResult : IFunctionAnalyzationResult, IMemberAnalyzationResult
	{
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
		/// References to other methods that are referenced in trivias
		/// </summary>
		IReadOnlyList<IFunctionReferenceAnalyzationResult> CrefMethodReferences { get; }

		/// <summary>
		/// When true, the method has at least one invocation that needs a <see cref="CancellationToken"/> as a parameter.
		/// </summary>
		bool CancellationTokenRequired { get; }

		/// <summary>
		/// When true, the method body must be wrapped within a async lock as <see cref="MethodImplOptions.Synchronized"/> 
		/// is not supported for async methods
		/// </summary>
		bool MustRunSynchronized { get; }
	}
}
