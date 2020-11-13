﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class AssemblyExtensions
	{
		public static string GetPath(this Assembly assembly)
		{
#if LEGACY
			var path = assembly.CodeBase;
#else
			var path = assembly.Location;
#endif
			if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
			{
				throw new InvalidOperationException($"Invalid assembly location: '{assembly.Location}'");
			}

			return Uri.UnescapeDataString(uri.AbsolutePath);
		}
	}
}
