using System;
using System.Threading;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectCancellationTokenConfiguration
	{
		/// <summary>
		/// Enable or disable inserting cancellation token guards for methods that will be generated with an additional <see cref="CancellationToken"/> parameter.
		/// <para>Default is set to false.</para>
		/// </summary>
		IFluentProjectCancellationTokenConfiguration Guards(bool value);

		/// <summary>
		/// Specify the desired generation of the additional <see cref="CancellationToken"/> parameter for the given method
		/// </summary>
		IFluentProjectCancellationTokenConfiguration MethodGeneration(Func<IMethodSymbolInfo, MethodCancellationToken> func);

		/// <summary>
		/// Override the default behavior for calculating the requirement of the cancellation token for the given method.
		/// </summary>
		IFluentProjectCancellationTokenConfiguration RequiresCancellationToken(Func<IMethodSymbol, bool?> func);
	}
}
