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

		public static void RunVoid(Action action)
		{
			action();
		}

		public static void RunGeneric<TValue>(Func<TValue> action)
		{
			action();
		}

		public static void RunWithParameter(Action<object> action)
		{
		}

		public static void RunWithParameter(Func<object, Task> func)
		{
		}

		public static void RunWithIntParameter(Action<int> action)
		{
		}

		public static void RunWithIntParameterAsync(Func<int, Task> func)
		{
		}
	}
}
