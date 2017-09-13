using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IFunctionAnalyzationResult : IAnalyzationResult
	{
		/// <summary>
		/// Symbol of the function
		/// </summary>
		IMethodSymbol Symbol { get; }

		MethodConversion Conversion { get; }

		/// <summary>
		/// Get the syntax node of the function body
		/// </summary>
		SyntaxNode GetBodyNode();

		/// <summary>
		/// Get the method or accessor that contains this function
		/// </summary>
		/// <returns></returns>
		IMethodOrAccessorAnalyzationResult GetMethodOrAccessor();

		/// <summary>
		/// References of types that are used inside this function
		/// </summary>
		IReadOnlyList<ITypeReferenceAnalyzationResult> TypeReferences { get; }

		/// <summary>
		/// References to other methods that are referenced inside this function
		/// </summary>
		IReadOnlyList<IFunctionReferenceAnalyzationResult> FunctionReferences { get; }

		/// <summary>
		/// References to other methods that are referenced/invoked inside this function and are candidates to be async
		/// </summary>
		IEnumerable<IBodyFunctionReferenceAnalyzationResult> BodyFunctionReferences { get; }

		/// <summary>
		/// Statements inside the function that were qualified as preconditions. Preconditions may be filled only for functions that 
		/// are going to be converted
		/// </summary>
		IReadOnlyList<StatementSyntax> Preconditions { get; }

		/// <summary>
		/// Anonymous/local functions that are declared inside this function/method
		/// </summary>
		IReadOnlyList<IChildFunctionAnalyzationResult> ChildFunctions { get; }

		/// <summary>
		/// Contains lock statements information inside the function
		/// </summary>
		IReadOnlyList<ILockAnalyzationResult> Locks { get; }

		/// <summary>
		/// When true, the async keyword will be omitted
		/// </summary>
		bool OmitAsync { get; }

		/// <summary>
		/// When true, the return type of the generated method will not be wrapped into a <see cref="Task{TResult}"/>
		/// </summary>
		bool PreserveReturnType { get; }

		/// <summary>
		/// When true, the method will be splitted into two. 
		/// </summary>
		bool SplitTail { get; }

		/// <summary>
		/// When true, the method will be wrapped in a try/catch block
		/// </summary>
		bool WrapInTryCatch { get; }

		/// <summary>
		/// When true, the first statement (preconditions omitted) is a throw statement (eg. not implemented methods)
		/// </summary>
		bool Faulted { get; }

		/// <summary>
		/// When true, yield statements in method body will be rewritten to return statements
		/// </summary>
		bool RewriteYields { get; }

		/// <summary>
		/// The name what will be used for the async counterpart
		/// </summary>
		string AsyncCounterpartName { get; }
	}
}
