using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public class ClassWithLocalAsyncMethods
	{
		public void Work()
		{
			LocalMethodWithCancellation();
			LocalMethodWithoutCancellation();
			LocalMethodWithCancellation();
		}

		public void LocalMethodWithCancellation()
		{
			Thread.Sleep(100);
		}
		public void LocalMethodWithoutCancellation()
		{
			Thread.Sleep(100);
		}

		public async Task LocalMethodWithCancellationAsync(CancellationToken cancellationToken)
		{
			await Task.Delay(100, cancellationToken);
		}
		public async Task LocalMethodWithoutCancellationAsync()
		{
			await Task.Delay(100);
		}
	}
}