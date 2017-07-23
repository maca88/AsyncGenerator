using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectAnalyzeConfiguration
	{
		/// <summary>
		/// Set a function that will decide what type of conversion to apply for a given method.
		/// <para>Default <see cref="F:AsyncGenerator.Core.MethodConversion.Unknown"/> is chosen for all methods.</para> 
		/// </summary>
		IFluentProjectAnalyzeConfiguration MethodConversion(Func<IMethodSymbol, MethodConversion> func);

		/// <summary>
		/// Enable or disable generating async counterparts for property accessors (getter and setters).
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration PropertyConversion(bool value);

		/// <summary>
		/// Set a function that will decide what type of conversion to apply for a given type.
		/// <para>Default <see cref="F:AsyncGenerator.Core.TypeConversion.Unknown"/> is chosen for all types.</para> 
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
		IFluentProjectAnalyzeConfiguration FindAsyncCounterparts(Func<IMethodSymbol, ITypeSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func);

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
		/// Enable or disable scanning and generating async counterparts with an additional parameter of type <see cref="System.Threading.CancellationToken"/>.
		/// When true, the <see cref="AsyncCounterpartsSearchOptions.HasCancellationToken"/> option will be passed for all registered async counterpart finders.
		/// For more control over the generation use the overload with the action parameter.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration CancellationTokens(bool value);

		/// <summary>
		/// Enables scanning and generating async counterparts with an additional parameter of type <see cref="System.Threading.CancellationToken"/>.
		/// The <see cref="AsyncCounterpartsSearchOptions.HasCancellationToken"/> option will be passed for all registered async counterpart finders.
		/// </summary>
		/// <returns></returns>
		IFluentProjectAnalyzeConfiguration CancellationTokens(Action<IFluentProjectCancellationTokenConfiguration> action);

		/// <summary>
		/// Register the location of the async extension methods
		/// </summary>
		IFluentProjectAnalyzeConfiguration AsyncExtensionMethods(Action<IFluentProjectAsyncExtensionMethodsConfiguration> action);

		/// <summary>
		/// Set the predicate that will decide whether the return type of an async method should be preserved or not.
		/// The predicate will be called only for methods that do not have any async invocation that returns a <see cref="Task"/>
		/// <para>Default false is choosen for all methods.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration PreserveReturnType(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Set the predicate that will decide whether to search async counterparts for the given method. 
		/// Use this option when an external method is not wanted to be async.
		/// <para>Default true is choosen for all methods.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration SearchForAsyncCounterparts(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Enable or disable scanning for missing async counterparts.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanForMissingAsyncMembers(bool value);

		/// <summary>
		/// Conditionally scan for missing async counterparts based on the given predicate.
		/// </summary>
		IFluentProjectAnalyzeConfiguration ScanForMissingAsyncMembers(Predicate<INamedTypeSymbol> predicate);
		

		/// <summary>
		/// Appends a callback that will be called after the analyzation step.
		/// </summary>
		IFluentProjectAnalyzeConfiguration AfterAnalyzation(Action<IProjectAnalyzationResult> action);

	}
}
