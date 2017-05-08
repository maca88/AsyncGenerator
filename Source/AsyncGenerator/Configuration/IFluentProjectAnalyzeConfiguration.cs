using System;
using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public enum CancellationTokenMethodGeneration
	{
		WithDefaultParameter = 0,
		WithParameter,
		WithAndWithoutParameter
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
		CancellationTokenMethodGeneration GetMethodGeneration(IMethodSymbolInfo methodSymbolInfo);
	}

	public class DefaultCancellationTokenMethodGenerationBehavior : ICancellationTokenMethodGenerationBehavior
	{
		public CancellationTokenMethodGeneration GetMethodGeneration(IMethodSymbolInfo methodSymbolInfo)
		{
			return CancellationTokenMethodGeneration.WithDefaultParameter;
		}
	}

	public class CustomCancellationTokenMethodGenerationBehavior : ICancellationTokenMethodGenerationBehavior
	{
		public CancellationTokenMethodGeneration GetMethodGeneration(IMethodSymbolInfo methodSymbolInfo)
		{
			// If this is an virtual or abstract method, then generate virtual or abstract method with cancellation token and a sealed (non virtual) 
			// method which calls the virtual method with the default cancellation token (None).
			if (methodSymbolInfo.Symbol.IsVirtual || methodSymbolInfo.Symbol.IsAbstract)
			{
				// TODO: sealed option
				return CancellationTokenMethodGeneration.WithAndWithoutParameter;
			}
			// If it is an interface, then generate only method with cancellation token.
			if (methodSymbolInfo.Symbol.ContainingType.TypeKind == TypeKind.Interface || methodSymbolInfo.OverridenMethods.Any())
			{
				return CancellationTokenMethodGeneration.WithParameter;
			}

			return CancellationTokenMethodGeneration.WithAndWithoutParameter;
		}
	}

	public interface IFluentProjectCancellationTokenConfiguration
	{
		IFluentProjectCancellationTokenConfiguration Guards(bool value);

		IFluentProjectCancellationTokenConfiguration MethodGeneration(Func<IMethodSymbolInfo, CancellationTokenMethodGeneration> func);
	}

	internal class FluentProjectCancellationTokenConfiguration : IFluentProjectCancellationTokenConfiguration
	{
		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbolInfo, CancellationTokenMethodGeneration> MethodGeneration { get; private set; } = symbol => CancellationTokenMethodGeneration.WithDefaultParameter;

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.Guards(bool value)
		{
			Guards = value;
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.MethodGeneration(Func<IMethodSymbolInfo, CancellationTokenMethodGeneration> func)
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


		IFluentProjectAnalyzeConfiguration CancellationTokens(Action<IFluentProjectCancellationTokenConfiguration> action);

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
