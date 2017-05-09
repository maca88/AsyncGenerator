using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	[Flags]
	public enum CancellationTokenMethod
	{
		/// <summary>
		/// Generates one method with an additional optional <see cref="CancellationToken"/> parameter. This option cannot be combined with other options.
		/// </summary>
		DefaultParameter = 1,
		/// <summary>
		/// Generates one method with an additional required <see cref="CancellationToken"/> parameter.
		/// </summary>
		Parameter = 2,
		/// <summary>
		/// Generates one overload method without additional parameters that will forward the call to a method with an additional <see cref="CancellationToken"/> parameter
		/// using <see cref="CancellationToken.None"/> as argument.
		/// This option shall be combined with <see cref="Parameter"/> option.
		/// </summary>
		NoParameterForward = 4,
		/// <summary>
		/// The same as <see cref="NoParameterForward"/> with the addition that the sealed keyword will be added for overrides and virtual keyword removed from the virtual methods.
		/// This option shall be combined with <see cref="Parameter"/> option.
		/// </summary>
		SealedNoParameterForward = 8
	}

	public interface IMethodSymbolInfo
	{
		IMethodSymbol Symbol { get; }

		IReadOnlyList<IMethodSymbol> ImplementedInterfaces { get; }

		IReadOnlyList<IMethodSymbol> OverridenMethods { get; }

		IMethodSymbol BaseOverriddenMethod { get; }
	}

	public interface ICancellationTokenMethodGenerationBehavior
	{
		CancellationTokenMethod GetMethodGeneration(IMethodSymbolInfo methodSymbolInfo);
	}

	public interface IFluentProjectCancellationTokenConfiguration
	{
		IFluentProjectCancellationTokenConfiguration Guards(bool value);

		IFluentProjectCancellationTokenConfiguration MethodGeneration(Func<IMethodSymbol, CancellationTokenMethod> func);
	}

	public interface IProjectCancellationTokenConfiguration
	{
		bool Guards { get; }

		Func<IMethodSymbol, CancellationTokenMethod> MethodGeneration { get; }
	}

	internal class FluentProjectCancellationTokenConfiguration : IFluentProjectCancellationTokenConfiguration, IProjectCancellationTokenConfiguration
	{
		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbol, CancellationTokenMethod> MethodGeneration { get; private set; } = symbol => CancellationTokenMethod.DefaultParameter;

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.Guards(bool value)
		{
			Guards = value;
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.MethodGeneration(Func<IMethodSymbol, CancellationTokenMethod> func)
		{
			MethodGeneration = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}
	}

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
