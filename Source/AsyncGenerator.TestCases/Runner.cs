using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class Runner
	{
		public static void Run(Action<string> action)
		{
		}

		public static Task RunAsync(Action<string> action)
		{
			return Task.CompletedTask;
		}
	}
}
