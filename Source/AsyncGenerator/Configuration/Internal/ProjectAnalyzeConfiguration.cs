using System;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAnalyzeConfiguration : IFluentProjectAnalyzeConfiguration, IProjectAnalyzeConfiguration
	{
		public Func<IMethodSymbol, MethodConversion> MethodConversionFunction { get; private set; } = m => MethodConversion.Unknown;

		public Func<INamedTypeSymbol, TypeConversion> TypeConversionFunction { get; private set; } = m => TypeConversion.Unknown;

		public Predicate<Document> DocumentSelectionPredicate { get; private set; } = m => true;

		public List<IAsyncCounterpartsFinder> FindAsyncCounterpartsFinders { get; } = new List<IAsyncCounterpartsFinder>();

		public List<IPreconditionChecker> PreconditionCheckers { get; } = new List<IPreconditionChecker>();

		public List<IInvocationExpressionAnalyzer> InvocationExpressionAnalyzers { get; } = new List<IInvocationExpressionAnalyzer>();

		public List<Action<IProjectAnalyzationResult>> AfterAnalyzation { get; } = new List<Action<IProjectAnalyzationResult>>();

		public bool ScanMethodBody { get; private set; }

		public bool ScanForMissingAsyncMembers { get; private set; }

		public bool UseCancellationTokenOverload { get; private set; }

		#region IFluentProjectAnalyzeConfiguration

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			MethodConversionFunction = func ?? throw new ArgumentNullException(nameof(func));
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

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.FindAsyncCounterparts(Func<IMethodSymbol, AsyncCounterpartsSearchOptions, IEnumerable<IMethodSymbol>> func)
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

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(bool value)
		{
			ScanForMissingAsyncMembers = value;
			return this;
		}

		IFluentProjectAnalyzeConfiguration IFluentProjectAnalyzeConfiguration.UseCancellationTokenOverload(bool value)
		{
			UseCancellationTokenOverload = value;
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

	}
}
