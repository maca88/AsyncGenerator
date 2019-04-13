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
		public ProjectCancellationTokenConfiguration()
		{
			RequiresCancellationToken = CreateRequiresCancellationTokenFunction(symbol => null);
		}

		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbolInfo, MethodCancellationToken> MethodGeneration { get; private set; } =
			symbolInfo => symbolInfo.Symbol.ExplicitInterfaceImplementations.Any()
				? MethodCancellationToken.Required
				: MethodCancellationToken.Optional;

		public Func<IMethodSymbol, bool?> RequiresCancellationToken { get; private set; }

		public List<IMethodRequiresCancellationTokenProvider> MethodRequiresCancellationTokenProviders { get; } = new List<IMethodRequiresCancellationTokenProvider>();

		private Func<IMethodSymbol, bool?> CreateRequiresCancellationTokenFunction(Func<IMethodSymbol, bool?> defaultFunc)
		{
			return Function;

			bool? Function(IMethodSymbol methodSymbol)
			{
				foreach (var provider in MethodRequiresCancellationTokenProviders)
				{
					var value = provider.RequiresCancellationToken(methodSymbol);
					if (value.HasValue)
					{
						return value.Value;
					}
				}

				return defaultFunc(methodSymbol);
			}
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
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			RequiresCancellationToken = CreateRequiresCancellationTokenFunction(func);
			return this;
		}

		#endregion
	}
}
