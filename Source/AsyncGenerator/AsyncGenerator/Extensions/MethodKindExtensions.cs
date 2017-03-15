using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Extensions
{
	internal static class MethodKindExtensions
	{
		public static bool IsPropertyAccessor(this MethodKind kind)
		{
			return kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;
		}
	}
}
