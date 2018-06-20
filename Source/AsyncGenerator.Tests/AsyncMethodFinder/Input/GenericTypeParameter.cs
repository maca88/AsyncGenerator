using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class GenericTypeParameter
	{
		public void Test(GenericTypeParameter<bool> test)
		{
			test.Execute(() =>
			{
				SimpleFile.Read();
				return true;
			});
		}
	}

	public class GenericTypeParameter<TResult>
	{
		public TResult Execute(Func<TResult> func)
		{
			return default(TResult);
		}

		public Task<TResult> ExecuteAsync(Func<Task<TResult>> func, CancellationToken cancellationToken)
		{
			return Task.FromResult(default(TResult));
		}
	}
}
