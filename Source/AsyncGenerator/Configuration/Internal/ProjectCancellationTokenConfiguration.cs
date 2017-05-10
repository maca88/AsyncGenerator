using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectCancellationTokenConfiguration : IFluentProjectCancellationTokenConfiguration, IProjectCancellationTokenConfiguration
	{
		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbol, MethodCancellationToken> MethodGeneration { get; private set; } = symbol => MethodCancellationToken.DefaultParameter;

		public Func<IMethodSymbol, bool?> RequireCancellationToken { get; private set; } = symbol => null;

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.Guards(bool value)
		{
			Guards = value;
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.MethodGeneration(Func<IMethodSymbol, MethodCancellationToken> func)
		{
			MethodGeneration = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.RequireCancellationToken(Func<IMethodSymbol, bool?> func)
		{
			RequireCancellationToken = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}
	}
}
