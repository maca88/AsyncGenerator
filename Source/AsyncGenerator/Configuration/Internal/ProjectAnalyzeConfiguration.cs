using System;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Internal;
using AsyncGenerator.Plugins;
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
		}

		public Func<IMethodSymbol, MethodConversion> MethodConversionFunction { get; private set; } = m => MethodConversion.Unknown;

		public bool PropertyConversion { get; private set; }

		public Func<INamedTypeSymbol, TypeConversion> TypeConversionFunction { get; private set; } = m => TypeConversion.Unknown;

		public Predicate<Document> DocumentSelectionPredicate { get; private set; } = m => true;

		public Predicate<IMethodSymbol> PreserveReturnType { get; private set; } = m => false;

		public Predicate<IMethodSymbol> SearchForAsyncCounterparts { get; private set; } = m => true;

		public Predicate<IMethodSymbol> SearchForMethodReferences { get; private set; } = m => true;

		public List<IAsyncCounterpartsFinder> FindAsyncCounterpartsFinders { get; } = new List<IAsyncCounterpartsFinder>();

		public List<IPreconditionChecker> PreconditionCheckers { get; } = new List<IPreconditionChecker>();

		public List<IInvocationExpressionAnalyzer> InvocationExpressionAnalyzers { get; } = new List<IInvocationExpressionAnalyzer>();

		public List<Action<IProjectAnalyzationResult>> AfterAnalyzation { get; } = new List<Action<IProjectAnalyzationResult>>();

		public ProjectCancellationTokenConfiguration CancellationTokens { get; } = new ProjectCancellationTokenConfiguration();

		public ProjectAsyncExtensionMethodsConfiguration AsyncExtensionMethods { get; } = new ProjectAsyncExtensionMethodsConfiguration();

		public bool ScanMethodBody { get; private set; }

		public Predicate<INamedTypeSymbol> ScanForMissingAsyncMembers { get; private set; }

		public bool UseCancellationTokens => CancellationTokens.Enabled;

		public bool ConcurrentRun => _projectConfiguration.ConcurrentRun;

		public Predicate<IMethodSymbol> CallForwarding { get; set; } = symbol => false;

		#region IFluentProjectAnalyzeConfiguration

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			MethodConversionFunction = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PropertyConversion(bool value)
		{
			PropertyConversion = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func)
		{
			TypeConversionFunction = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.DocumentSelection(Predicate<Document> predicate)
		{
			DocumentSelectionPredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.FindAsyncCounterparts(Func<IMethodSymbol, ITypeSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			FindAsyncCounterpartsFinders.Add(new DelegateAsyncCounterpartsFinder(func));
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

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CallForwarding(bool value)
		{
			CallForwarding = symbol => value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.CallForwarding(Predicate<IMethodSymbol> predicate)
		{
			CallForwarding = predicate;
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
			CancellationTokens.Enabled = true;
			action(AsyncExtensionMethods);
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.PreserveReturnType(Predicate<IMethodSymbol> predicate)
		{
			PreserveReturnType = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForAsyncCounterparts(Predicate<IMethodSymbol> predicate)
		{
			SearchForAsyncCounterparts = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.SearchForMethodReferences(Predicate<IMethodSymbol> predicate)
		{
			SearchForMethodReferences = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(bool value)
		{
			if (value)
			{
				ScanForMissingAsyncMembers = symbol => true;
			}
			else
			{
				ScanForMissingAsyncMembers = null;
			}
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(Predicate<INamedTypeSymbol> predicate)
		{
			ScanForMissingAsyncMembers = predicate;
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

		#endregion

	}
}
