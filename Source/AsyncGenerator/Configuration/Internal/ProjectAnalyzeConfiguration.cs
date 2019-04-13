using System;
using System.Collections.Generic;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAnalyzeConfiguration : IFluentProjectAnalyzeConfiguration, IProjectAnalyzeConfiguration
	{
		private readonly IProjectConfiguration _projectConfiguration;

		public ProjectAnalyzeConfiguration(IProjectConfiguration projectConfiguration)
		{
			_projectConfiguration = projectConfiguration;
			GetMethodConversion = CreateMethodConversionFunction(m => MethodConversion.Unknown);
			GetTypeConversion = CreateTypeConversionFunction(m => TypeConversion.Unknown);
			CanPreserveReturnType = CreatePreserveReturnTypeFunction(m => false);
			CanAlwaysAwait = CreateAlwaysAwaitFunction(symbol => false);
			CanSearchForMethodReferences = CreateSearchForMethodReferencesFunction(m => true);
		}

		public Func<IMethodSymbol, MethodConversion> GetMethodConversion { get; private set; }

		public Func<INamedTypeSymbol, TypeConversion> GetTypeConversion { get; private set; }

		public Predicate<Document> CanSelectDocument { get; private set; } = m => true;

		public Predicate<IMethodSymbol> CanPreserveReturnType { get; private set; }

		public Predicate<IMethodSymbol> CanSearchForAsyncCounterparts { get; private set; } = m => true;

		public Predicate<IMethodSymbol> CanSearchForMethodReferences { get; private set; }

		public Predicate<IMethodSymbol> CanAlwaysAwait { get; private set; }

		public Predicate<INamedTypeSymbol> CanScanForMissingAsyncMembers { get; private set; }

		public Predicate<IMethodSymbol> CanForwardCall { get; set; } = symbol => false;


		public bool PropertyConversion { get; private set; }

		public bool SearchAsyncCounterpartsInInheritedTypes { get; private set; }

		public bool ScanMethodBody { get; private set; }

		public bool UseCancellationTokens => CancellationTokens.Enabled;

		public bool ConcurrentRun => _projectConfiguration.ConcurrentRun;


		public List<IAsyncCounterpartsFinder> AsyncCounterpartsFinders { get; } = new List<IAsyncCounterpartsFinder>();

		public List<IPreserveMethodReturnTypeProvider> PreserveMethodReturnTypeProviders { get; } = new List<IPreserveMethodReturnTypeProvider>();

		public List<IMethodConversionProvider> MethodConversionProviders { get; } = new List<IMethodConversionProvider>();

		public List<ITypeConversionProvider> TypeConversionProviders { get; } = new List<ITypeConversionProvider>();

		public List<IAlwaysAwaitMethodProvider> AlwaysAwaitMethodProviders { get; } = new List<IAlwaysAwaitMethodProvider>();

		public List<ISearchForMethodReferencesProvider> SearchForMethodReferencesProviders { get; } = new List<ISearchForMethodReferencesProvider>();

		public List<IMethodExceptionHandler> MethodExceptionHandlers { get; } = new List<IMethodExceptionHandler>();

		public List<Predicate<IMethodSymbol>> IgnoreAsyncCounterpartsPredicates { get; } = new List<Predicate<IMethodSymbol>>();

		public List<IPreconditionChecker> PreconditionCheckers { get; } = new List<IPreconditionChecker>();

		public List<IInvocationExpressionAnalyzer> InvocationExpressionAnalyzers { get; } = new List<IInvocationExpressionAnalyzer>();

		public List<IBodyFunctionReferencePostAnalyzer> BodyFunctionReferencePostAnalyzers { get; } = new List<IBodyFunctionReferencePostAnalyzer>();

		public List<Action<IProjectAnalyzationResult>> AfterAnalyzation { get; } = new List<Action<IProjectAnalyzationResult>>();


		public ProjectCancellationTokenConfiguration CancellationTokens { get; } = new ProjectCancellationTokenConfiguration();

		public ProjectAsyncExtensionMethodsConfiguration AsyncExtensionMethods { get; } = new ProjectAsyncExtensionMethodsConfiguration();

		public ProjectDiagnosticsConfiguration Diagnostics { get; } = new ProjectDiagnosticsConfiguration();

		public ProjectExceptionHandlingConfiguration ExceptionHandling { get; } = new ProjectExceptionHandlingConfiguration();


		private Predicate<IMethodSymbol> CreatePreserveReturnTypeFunction(Predicate<IMethodSymbol> defaultFunc)
		{
			return Function;

			bool Function(IMethodSymbol methodSymbol)
			{
				foreach (var provider in PreserveMethodReturnTypeProviders)
				{
					var value = provider.PreserveReturnType(methodSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(methodSymbol);
			}
		}

		private Func<IMethodSymbol, MethodConversion> CreateMethodConversionFunction(Func<IMethodSymbol, MethodConversion> defaultFunc)
		{
			return Function;

			MethodConversion Function(IMethodSymbol methodSymbol)
			{
				foreach (var provider in MethodConversionProviders)
				{
					var value = provider.GetConversion(methodSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(methodSymbol);
			}
		}

		private Func<INamedTypeSymbol, TypeConversion> CreateTypeConversionFunction(Func<INamedTypeSymbol, TypeConversion> defaultFunc)
		{
			return Function;

			TypeConversion Function(INamedTypeSymbol typeSymbol)
			{
				foreach (var provider in TypeConversionProviders)
				{
					var value = provider.GetConversion(typeSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(typeSymbol);
			}
		}

		private Predicate<IMethodSymbol> CreateAlwaysAwaitFunction(Predicate<IMethodSymbol> defaultFunc)
		{
			return Function;

			bool Function(IMethodSymbol methodSymbol)
			{
				foreach (var provider in AlwaysAwaitMethodProviders)
				{
					var value = provider.AlwaysAwait(methodSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(methodSymbol);
			}
		}

		private Predicate<IMethodSymbol> CreateSearchForMethodReferencesFunction(Predicate<IMethodSymbol> defaultFunc)
		{
			return Function;

			bool Function(IMethodSymbol methodSymbol)
			{
				foreach (var provider in SearchForMethodReferencesProviders)
				{
					var value = provider.SearchForMethodReferences(methodSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(methodSymbol);
			}
		}

		#region IFluentProjectAnalyzeConfiguration

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			GetMethodConversion = CreateMethodConversionFunction(func);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PropertyConversion(bool value)
		{
			PropertyConversion = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			GetTypeConversion = CreateTypeConversionFunction(func);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.DocumentSelection(Predicate<Document> predicate)
		{
			CanSelectDocument = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.FindAsyncCounterparts(Func<IMethodSymbol, ITypeSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			AsyncCounterpartsFinders.Add(new DelegateAsyncCounterpartsFinder(func));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.IgnoreAsyncCounterparts(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			IgnoreAsyncCounterpartsPredicates.Add(predicate);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.IsPrecondition(Func<StatementSyntax, SemanticModel, bool> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			PreconditionCheckers.Add(new DelegatePreconditionChecker(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanMethodBody(bool value)
		{
			ScanMethodBody = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchAsyncCounterpartsInInheritedTypes(bool value)
		{
			SearchAsyncCounterpartsInInheritedTypes = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CallForwarding(bool value)
		{
			CanForwardCall = symbol => value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CallForwarding(Predicate<IMethodSymbol> predicate)
		{
			CanForwardCall = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AlwaysAwait(bool value)
		{
			CanAlwaysAwait = CreateAlwaysAwaitFunction(symbol => value);
			return this; 
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AlwaysAwait(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			CanAlwaysAwait = CreateAlwaysAwaitFunction(predicate);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CancellationTokens(bool value)
		{
			CancellationTokens.Enabled = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CancellationTokens(Action<IFluentProjectCancellationTokenConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			CancellationTokens.Enabled = true;
			action(CancellationTokens);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AsyncExtensionMethods(Action<IFluentProjectAsyncExtensionMethodsConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(AsyncExtensionMethods);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.Diagnostics(Action<IFluentProjectDiagnosticsConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(Diagnostics);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ExceptionHandling(Action<IFluentProjectExceptionHandlingConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(ExceptionHandling);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PreserveReturnType(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			CanPreserveReturnType = CreatePreserveReturnTypeFunction(predicate);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForAsyncCounterparts(Predicate<IMethodSymbol> predicate)
		{
			CanSearchForAsyncCounterparts = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForMethodReferences(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			CanSearchForMethodReferences = CreateSearchForMethodReferencesFunction(predicate);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(bool value)
		{
			if (value)
			{
				CanScanForMissingAsyncMembers = symbol => true;
			}
			else
			{
				CanScanForMissingAsyncMembers = null;
			}
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(Predicate<INamedTypeSymbol> predicate)
		{
			CanScanForMissingAsyncMembers = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AfterAnalyzation(Action<IProjectAnalyzationResult> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			AfterAnalyzation.Add(action);
			return this;
		}

		#endregion

		#region IProjectAnalyzeConfiguration

		IProjectCancellationTokenConfiguration IProjectAnalyzeConfiguration.CancellationTokens => CancellationTokens;

		IProjectExceptionHandlingConfiguration IProjectAnalyzeConfiguration.ExceptionHandling => ExceptionHandling;

		#endregion

	}
}
