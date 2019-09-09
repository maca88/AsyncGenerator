using System;
using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectCancellationTokenConfiguration : IFluentProjectCancellationTokenConfiguration, IProjectCancellationTokenConfiguration
	{
		private Func<IMethodSymbol, bool?> _requiresCancellationTokenFunction;
		private Func<IMethodSymbol, bool?> _lastRequiresCancellationTokenFunction;

		public ProjectCancellationTokenConfiguration()
		{
		}

		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbolInfo, MethodCancellationToken> MethodGeneration { get; private set; } =
			symbolInfo => symbolInfo.Symbol.ExplicitInterfaceImplementations.Any()
				? MethodCancellationToken.Required
				: MethodCancellationToken.Optional;

		public List<IMethodRequiresCancellationTokenProvider> MethodRequiresCancellationTokenProviders { get; } = new List<IMethodRequiresCancellationTokenProvider>();

		public bool? RequiresCancellationToken(IMethodSymbol methodSymbol)
		{
			var result = _requiresCancellationTokenFunction?.Invoke(methodSymbol);
			if (result.HasValue)
			{
				return result.Value;
			}

			foreach (var provider in MethodRequiresCancellationTokenProviders)
			{
				result = provider.RequiresCancellationToken(methodSymbol);
				if (result.HasValue)
				{
					return result.Value;
				}
			}

			return _lastRequiresCancellationTokenFunction?.Invoke(methodSymbol) ?? null;
		}

		#region IProjectCancellationTokenConfiguration

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.Guards(bool value)
		{
			Guards = value;
			return this;
		}

		#endregion

		#region IFluentProjectCancellationTokenConfiguration

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.ParameterGeneration(Func<IMethodSymbolInfo, MethodCancellationToken> func)
		{
			MethodGeneration = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.RequiresCancellationToken(Func<IMethodSymbol, bool?> func)
		{
			return ((IFluentProjectCancellationTokenConfiguration)this).RequiresCancellationToken(func, ExecutionPhase.Default);
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.RequiresCancellationToken(Func<IMethodSymbol, bool?> func, ExecutionPhase executionPhase)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			switch (executionPhase)
			{
				case ExecutionPhase.Default:
					_requiresCancellationTokenFunction = func;
					break;
				case ExecutionPhase.PostProviders:
					_lastRequiresCancellationTokenFunction = func;
					break;
			}

			return this;
		}

		#endregion
	}
}
