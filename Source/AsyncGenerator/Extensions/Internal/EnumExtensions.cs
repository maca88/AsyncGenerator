using System;
using System.Linq;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class EnumExtensions
	{
		public static bool HasAnyFlag(this Enum e, params Enum[] enums)
		{
			return enums.Any(e.HasFlag);
		}

		public static bool HasOptionalCancellationToken(this MethodCancellationToken e)
		{
			return e.HasAnyFlag(
				MethodCancellationToken.DefaultParameter,
				MethodCancellationToken.NoParameterForward,
				MethodCancellationToken.SealedNoParameterForward);
		}
	}
}
