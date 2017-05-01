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
		/// Set a function that will decide what type of conversion to apply for a given method
		/// </summary>
		IFluentProjectAnalyzeConfiguration MethodConversion(Func<IMethodSymbol, MethodConversion> func);

		/// <summary>
		/// Set a function that will decide what type of conversion to apply for a given type
		/// </summary>
		IFluentProjectAnalyzeConfiguration TypeConversion(Func<INamedTypeSymbol, TypeConversion> func);

		/// <summary>
		/// Set a predicate that will decide if the document will be analyzed
		/// </summary>
		IFluentProjectAnalyzeConfiguration DocumentSelection(Predicate<Document> predicate);

		/// <summary>
		/// Set a predicate that will decide if the method will be analyzed
		/// </summary>
		//IProjectAnalyzeConfiguration MethodSelectionPredicate(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Set a predicate that will decide if the type will be analyzed
		/// </summary>
		//IProjectAnalyzeConfiguration TypeSelectionPredicate(Predicate<INamedTypeSymbol> predicate);

		/// <summary>
		/// Set a predicate that will decide if the method that can be converted to async should be converted
		/// </summary>
		IFluentProjectAnalyzeConfiguration ConvertMethodPredicate(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Append a function that will try to find an async counterpart for the given method
		/// </summary>
		IFluentProjectAnalyzeConfiguration FindAsyncCounterparts(Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func);

		/// <summary>
		/// Append a predicate that will check if the given statement is a precondition
		/// </summary>
		IFluentProjectAnalyzeConfiguration IsPrecondition(Func<StatementSyntax, SemanticModel, bool> predicate);

		/// <summary>
		/// Enable or disable scanning for async counterparts within a method body
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanMethodBody(bool value);

		/// <summary>
		/// Enable or disable scanning for async counterparts with an additional parameter of type <see cref="System.Threading.CancellationToken"/>.
		/// When true, the <see cref="AsyncCounterpartsSearchOptions.HasCancellationToken"/> option will be passed for all registered async counterpart finders.
		/// </summary>
		IFluentProjectAnalyzeConfiguration UseCancellationTokenOverload(bool value);

		/// <summary>
		/// Enable or disable scanning for missing async counterparts
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanForMissingAsyncMembers(bool value);

		/// <summary>
		/// Appends a callback that will be called after the analyzation step
		/// </summary>
		IFluentProjectAnalyzeConfiguration AfterAnalyzation(Action<IProjectAnalyzationResult> action);

	}
}
