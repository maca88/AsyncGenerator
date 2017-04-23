using System;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAnalyzeConfiguration : IProjectAnalyzeConfiguration
	{
		public Func<IMethodSymbol, MethodConversion> MethodConversionFunction { get; private set; } = m => MethodConversion.Unknown;

		public Func<INamedTypeSymbol, TypeConversion> TypeConversionFunction { get; private set; } = m => TypeConversion.Unknown;

		public Predicate<Document> DocumentSelectionPredicate { get; private set; } = m => true;

		public Predicate<IMethodSymbol> ConvertMethodPredicate { get; private set; } = m => true;

		public List<IAsyncCounterpartsFinder> FindAsyncCounterpartsFinders { get; } = new List<IAsyncCounterpartsFinder>()
		{
			new DefaultAsyncCounterpartsFinder()
		};

		public List<IPreconditionChecker> PreconditionCheckers { get; } = new List<IPreconditionChecker>()
		{
			new DefaultPreconditionChecker()
		};

		public List<IInvocationExpressionAnalyzer> InvocationExpressionAnalyzers { get; } = new List<IInvocationExpressionAnalyzer>();

		public bool ScanMethodBody { get; private set; }

		public bool ScanForMissingAsyncMembers { get; private set; }

		public bool UseCancellationTokenOverload { get; private set; }

		public ProjectAnalyzeCallbacksConfiguration Callbacks { get; } = new ProjectAnalyzeCallbacksConfiguration();

		#region IProjectAnalyzeConfiguration

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			MethodConversionFunction = func;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			TypeConversionFunction = func;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.DocumentSelection(Predicate<Document> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			DocumentSelectionPredicate = predicate;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ConvertMethodPredicate(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			ConvertMethodPredicate = predicate;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.FindAsyncCounterparts(Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			FindAsyncCounterpartsFinders.Add(new DelegateAsyncCounterpartsFinder(func));
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.IsPrecondition(Func<StatementSyntax, SemanticModel, bool> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			PreconditionCheckers.Add(new DelegatePreconditionChecker(predicate));
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ScanMethodBody(bool value)
		{
			ScanMethodBody = value;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(bool value)
		{
			ScanForMissingAsyncMembers = value;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.UseCancellationTokenOverload(bool value)
		{
			UseCancellationTokenOverload = value;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.Callbacks(Action<IProjectAnalyzeCallbacksConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(Callbacks);
			return this;
		}

		#endregion

	}
}
