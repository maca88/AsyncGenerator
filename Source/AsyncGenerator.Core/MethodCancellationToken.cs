using System;
using System.Threading;

namespace AsyncGenerator.Core
{
	[Flags]
	public enum MethodCancellationToken
	{
		/// <summary>
		/// Generates one method with an additional optional <see cref="CancellationToken"/> parameter. This option cannot be combined with other options.
		/// </summary>
		DefaultParameter = 1,
		/// <summary>
		/// Generates one method with an additional required <see cref="CancellationToken"/> parameter.
		/// </summary>
		Parameter = 2,
		/// <summary>
		/// Generates one overload method without additional parameters that will forward the call to a method with an additional <see cref="CancellationToken"/> parameter
		/// using <see cref="CancellationToken.None"/> as argument.
		/// This option shall be combined with <see cref="Parameter"/> option.
		/// </summary>
		NoParameterForward = 4,
		/// <summary>
		/// The same as <see cref="NoParameterForward"/> with the addition that the sealed keyword will be added for overrides and virtual keyword removed from the virtual methods.
		/// This option shall be combined with <see cref="Parameter"/> option.
		/// </summary>
		SealedNoParameterForward = 8
	}
}
