using System;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public interface IFluentProjectAnalyzeConfiguration
	{
		/// <summary>
		/// Set a function that will decide what type of conversion to apply for a given method.
		/// <para>Default <see cref="F:AsyncGenerator.MethodConversion.Unknown"/> is chosen for all methods.</para> 
		/// </summary>
		IFluentProjectAnalyzeConfiguration MethodConversion(Func<IMethodSymbol, MethodConversion> func);

		/// <summary>
		/// Set a function that will decide what type of conversion to apply for a given type.
		/// <para>Default <see cref="F:AsyncGenerator.TypeConversion.Unknown"/> is chosen for all types.</para> 
		/// </summary>
		IFluentProjectAnalyzeConfiguration TypeConversion(Func<INamedTypeSymbol, TypeConversion> func);

		/// <summary>
		/// Set a predicate that will decide if the document will be analyzed.
		/// <para>Default all documents are anaylzed.</para> 
		/// </summary>
		IFluentProjectAnalyzeConfiguration DocumentSelection(Predicate<Document> predicate);

		/// <summary>
		/// Append a function that will try to find an async counterpart for the given method
		/// </summary>
		IFluentProjectAnalyzeConfiguration FindAsyncCounterparts(Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func);

		/// <summary>
		/// Append a predicate that will check if the given statement is a precondition
		/// </summary>
		IFluentProjectAnalyzeConfiguration IsPrecondition(Func<StatementSyntax, SemanticModel, bool> predicate);

		/// <summary>
		/// Enable or disable scanning for async counterparts within a method body.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanMethodBody(bool value);

		/// <summary>
		/// Enable or disable forwarding the call to the synchronous method instead of copying the whole body when the method does not have any async invocations.
		/// For fine tuning use the overload with a predicate.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration CallForwarding(bool value);

		/// <summary>
		/// Set the predicate that will decide when forwarding the call to the synchronous method instead of copying the whole body.
		/// The predicate will be called only for methods that do not have any async invocations.
		/// </summary>
		IFluentProjectAnalyzeConfiguration CallForwarding(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Enable or disable scanning for async counterparts with an additional parameter of type <see cref="System.Threading.CancellationToken"/>.
		/// When true, the <see cref="AsyncCounterpartsSearchOptions.HasCancellationToken"/> option will be passed for all registered async counterpart finders.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration UseCancellationTokenOverload(bool value);

		/// <summary>
		/// Enable or disable scanning for missing async counterparts.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanForMissingAsyncMembers(bool value);

		/// <summary>
		/// Appends a callback that will be called after the analyzation step.
		/// </summary>
		IFluentProjectAnalyzeConfiguration AfterAnalyzation(Action<IProjectAnalyzationResult> action);

	}
}
