using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class ActionFunc
	{
		public void Test()
		{
			Execute(val =>
			{
				SimpleFile.Read();
				SimpleFile.Read();
			});
		}

		private void Execute(Action<int> func)
		{
			SimpleFile.Read();
		}

		private Task ExecuteAsync(Func<int, Task> func)
		{
			return Task.CompletedTask;
		}
	}
}
