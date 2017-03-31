using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.Partial.TestCases
{
	public class MyTask
	{
		public static Task Run(Action action, Action action2 = null)
		{
			return Task.Run(action);
		}

		public static Task RunAsync(Func<Task> action, Action action2 = null)
		{
			return Task.Run(action);
		}

		public static Task RunAsync(Action action, Func<Task> action2 = null)
		{
			return Task.Run(action);
		}

		public static Task RunAsync(Func<Task> action, Func<Task> action2 = null)
		{
			return Task.Run(action);
		}

		public static Task RunAsync(Func<Task> action)
		{
			return Task.Run(action);
		}
	}

	public class MethodAsArgument
	{
		public void Run()
		{
			MyTask.Run(ReadFile);
			MyTask.Run(ReadFile, ReadFile);
		}

		public void ReadFile()
		{
			var stream = File.OpenRead("");
			stream.Read(new byte[0], 0, 0);
		}
	}
}
