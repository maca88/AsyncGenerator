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
		private Func<INamedTypeSymbol, TypeConversion> _typeConversionFunction; 
		private Func<INamedTypeSymbol, TypeConversion> _lastTypeConversionFunction;
		private Func<IMethodSymbol, MethodConversion> _methodConversionFunction;
		private Func<IMethodSymbol, MethodConversion> _lastMethodConversionFunction;
		private Func<IMethodSymbol, bool?> _preserveReturnTypeFunction;
		private Func<IMethodSymbol, bool?> _lastPreserveReturnTypeFunction;
		private Func<IMethodSymbol, bool?> _alwaysAwaitFunction;
		private Func<IMethodSymbol, bool?> _lastAlwaysAwaitFunction;
		private Func<IMethodSymbol, bool?> _searchForMethodReferencesFunction;
		private Func<IMethodSymbol, bool?> _lastSearchForMethodReferencesFunction;

		public ProjectAnalyzeConfiguration(IProjectConfiguration projectConfiguration)
		{
			_projectConfiguration = projectConfiguration;
		}

		public Predicate<Document> CanSelectDocument { get; private set; } = m => true;

		public Predicate<IMethodSymbol> CanSearchForAsyncCounterparts { get; private set; } = m => true;

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

		public List<IFieldConversionProvider> FieldConversionProviders { get; } = new List<IFieldConversionProvider>();

		public List<IAlwaysAwaitMethodProvider> AlwaysAwaitMethodProviders { get; } = new List<IAlwaysAwaitMethodProvider>();

		public List<ISearchForMethodReferencesProvider> SearchForMethodReferencesProviders { get; } = new List<ISearchForMethodReferencesProvider>();

		public List<IMethodExceptionHandler> MethodExceptionHandlers { get; } = new List<IMethodExceptionHandler>();

		public List<Predicate<IMethodSymbol>> IgnoreAsyncCounterpartsPredicates { get; } = new List<Predicate<IMethodSymbol>>();

		public List<IPreconditionChecker> PreconditionCheckers { get; } = new List<IPreconditionChecker>();

		public List<IInvocationExpressionAnalyzer> InvocationExpressionAnalyzers { get; } = new List<IInvocationExpressionAnalyzer>();

		public List<IBodyFunctionReferencePostAnalyzer> BodyFunctionReferencePostAnalyzers { get; } = new List<IBodyFunctionReferencePostAnalyzer>();

		public List<Action<IProjectAnalyzationResult>> AfterAnalyzation { get; } = new List<Action<IProjectAnalyzationResult>>();

		public List<Func<IMethodSymbol, AsyncReturnType?>> AsyncMethodReturnTypeFunctions { get; } = new List<Func<IMethodSymbol, AsyncReturnType?>>();


		public ProjectCancellationTokenConfiguration CancellationTokens { get; } = new ProjectCancellationTokenConfiguration();

		public ProjectAsyncExtensionMethodsConfiguration AsyncExtensionMethods { get; } = new ProjectAsyncExtensionMethodsConfiguration();

		public ProjectDiagnosticsConfiguration Diagnostics { get; } = new ProjectDiagnosticsConfiguration();

		public ProjectExceptionHandlingConfiguration ExceptionHandling { get; } = new ProjectExceptionHandlingConfiguration();


		public TypeConversion GetTypeConversion(INamedTypeSymbol typeSymbol)
		{
			var result = _typeConversionFunction?.Invoke(typeSymbol);
			if (result.HasValue && result != TypeConversion.Unknown)
			{
				return result.Value;
			}

			foreach (var provider in TypeConversionProviders)
			{
				result = provider.GetConversion(typeSymbol);
				if (result.HasValue)
				{
					return result.Value;
				}
			}

			return _lastTypeConversionFunction?.Invoke(typeSymbol) ?? TypeConversion.Unknown;
		}

		public MethodConversion GetMethodConversion(IMethodSymbol methodSymbol)
		{
			var result = _methodConversionFunction?.Invoke(methodSymbol);
			if (result.HasValue && result != MethodConversion.Unknown)
			{
				return result.Value;
			}

			foreach (var provider in MethodConversionProviders)
			{
				result = provider.GetConversion(methodSymbol);
				if (result.HasValue)
				{
					return result.Value;
				}
			}

			return _lastMethodConversionFunction?.Invoke(methodSymbol) ?? MethodConversion.Unknown;
		}

		public AsyncReturnType GetAsyncMethodReturnType(IMethodSymbol methodSymbol)
		{
			foreach (var func in AsyncMethodReturnTypeFunctions)
			{
				var value = func(methodSymbol);
				if (value.HasValue)
				{
					return value.Value;
				}
			}

			return AsyncReturnType.Task;
		}

		public FieldVariableConversion GetFieldConversion(ISymbol typeSymbol)
		{
			foreach (var provider in FieldConversionProviders)
			{
				var value = provider.GetFieldConversion(typeSymbol);
				if (value.HasValue)
				{
					return value.Value;
				}
			}

			return FieldVariableConversion.Unknown;
		}

		public bool CanPreserveReturnType(IMethodSymbol methodSymbol)
		{
			var result = _preserveReturnTypeFunction?.Invoke(methodSymbol);
			if (result.HasValue)
			{
				return result.Value;
			}

			foreach (var provider in PreserveMethodReturnTypeProviders)
			{
				var value = provider.PreserveReturnType(methodSymbol);
				if (value.HasValue)
				{
					return value.Value;
				}
			}

			return _lastPreserveReturnTypeFunction?.Invoke(methodSymbol) ?? false;
		}

		public bool CanAlwaysAwait(IMethodSymbol methodSymbol)
		{
			var result = _alwaysAwaitFunction?.Invoke(methodSymbol);
			if (result.HasValue)
			{
				return result.Value;
			}

			foreach (var provider in AlwaysAwaitMethodProviders)
			{
				var value = provider.AlwaysAwait(methodSymbol);
				if (value.HasValue)
				{
					return value.Value;
				}
			}

			return _lastAlwaysAwaitFunction?.Invoke(methodSymbol) ?? false;
		}

		public bool CanSearchForMethodReferences(IMethodSymbol methodSymbol)
		{
			var result = _searchForMethodReferencesFunction?.Invoke(methodSymbol);
			if (result.HasValue)
			{
				return result.Value;
			}

			foreach (var provider in SearchForMethodReferencesProviders)
			{
				var value = provider.SearchForMethodReferences(methodSymbol);
				if (value.HasValue)
				{
					return value.Value;
				}
			}

			return _lastSearchForMethodReferencesFunction?.Invoke(methodSymbol) ?? true;
		}

		#region IFluentProjectAnalyzeConfiguration

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			return ((IFluentProjectAnalyzeConfiguration)this).MethodConversion(func, ExecutionPhase.Default);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func, ExecutionPhase executionPhase)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_methodConversionFunction = func;
					break;
				case ExecutionPhase.PostProviders:
					_lastMethodConversionFunction = func;
					break;
			}

			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PropertyConversion(bool value)
		{
			PropertyConversion = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func)
		{
			return ((IFluentProjectAnalyzeConfiguration)this).TypeConversion(func, ExecutionPhase.Default);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func, ExecutionPhase executionPhase)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_typeConversionFunction = func;
					break;
				case ExecutionPhase.PostProviders:
					_lastTypeConversionFunction = func;
					break;
			}

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
			return ((IFluentProjectAnalyzeConfiguration)this).AlwaysAwait(symbol => value);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AlwaysAwait(Func<IMethodSymbol, bool?> predicate)
		{
			return ((IFluentProjectAnalyzeConfiguration)this).AlwaysAwait(predicate, ExecutionPhase.Default);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AlwaysAwait(Func<IMethodSymbol, bool?> predicate, ExecutionPhase executionPhase)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_alwaysAwaitFunction = predicate;
					break;
				case ExecutionPhase.PostProviders:
					_lastAlwaysAwaitFunction = predicate;
					break;
			}

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

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PreserveReturnType(Func<IMethodSymbol, bool?> predicate)
		{
			return ((IFluentProjectAnalyzeConfiguration)this).PreserveReturnType(predicate, ExecutionPhase.Default);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PreserveReturnType(Func<IMethodSymbol, bool?> predicate, ExecutionPhase executionPhase)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_preserveReturnTypeFunction = predicate;
					break;
				case ExecutionPhase.PostProviders:
					_lastPreserveReturnTypeFunction = predicate;
					break;
			}

			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.AsyncReturnType(Func<IMethodSymbol, AsyncReturnType?> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			AsyncMethodReturnTypeFunctions.Add(func);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForAsyncCounterparts(Predicate<IMethodSymbol> predicate)
		{
			CanSearchForAsyncCounterparts = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForMethodReferences(Func<IMethodSymbol, bool?> predicate)
		{
			return ((IFluentProjectAnalyzeConfiguration)this).SearchForMethodReferences(predicate, ExecutionPhase.Default);
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForMethodReferences(Func<IMethodSymbol, bool?> predicate, ExecutionPhase executionPhase)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_searchForMethodReferencesFunction = predicate;
					break;
				case ExecutionPhase.PostProviders:
					_lastSearchForMethodReferencesFunction = predicate;
					break;
			}

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
