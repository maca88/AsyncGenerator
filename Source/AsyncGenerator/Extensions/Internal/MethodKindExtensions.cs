using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class MethodKindExtensions
	{
		public static bool IsPropertyAccessor(this MethodKind kind)
		{
			return kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;
		}
	}
}
