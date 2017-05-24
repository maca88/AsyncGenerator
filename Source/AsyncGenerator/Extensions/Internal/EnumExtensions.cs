using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Extensions
{
	internal static class EnumExtensions
	{
		public static bool HasAnyFlag(this Enum e, params Enum[] enums)
		{
			return enums.Any(e.HasFlag);
		}
	}
}
