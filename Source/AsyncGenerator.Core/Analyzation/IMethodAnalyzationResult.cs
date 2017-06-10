using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IMethodAnalyzationResult : IFunctionAnalyzationResult, IMemberAnalyzationResult
	{
		MethodDeclarationSyntax Node { get; }

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
		/// When true, the method has at least one invocation that needs a <see cref="System.Threading.CancellationToken"/> as a parameter.
		/// </summary>
		bool CancellationTokenRequired { get; }

		/// <summary>
		/// When true, the method body must be wrapped within a async lock as <see cref="MethodImplOptions.Synchronized"/> 
		/// is not supported for async methods
		/// </summary>
		bool MustRunSynchronized { get; }

		/// <summary>
		/// When true, the async method will forward the call to the sync counterpart
		/// </summary>
		bool ForwardCall { get; }

		/// <summary>
		/// Specifies how shall the cancellation token parameter be generated for the method
		/// </summary>
		MethodCancellationToken? MethodCancellationToken { get; }

		/// <summary>
		/// When true, cancellation token guards will be inserted into the method
		/// </summary>
		bool AddCancellationTokenGuards { get; }

		// TODO: find a better way
		bool Missing { get; set; }
	}
}
