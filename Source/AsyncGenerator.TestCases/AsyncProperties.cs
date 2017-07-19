using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class AsyncProperties
	{
		public static bool Success { get; }

		public static Task<bool> GetSuccessAsync
		{
			get { return Task.FromResult(true); }
		}
	}
}
