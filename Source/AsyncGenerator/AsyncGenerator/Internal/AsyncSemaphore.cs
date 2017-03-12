using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncGenerator.Internal
{
	internal class AsyncSemaphore
	{
		private static readonly Task Completed = Task.FromResult(true);
		private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();
		private int _currentCount;

		public AsyncSemaphore(int initialCount)
		{
			if (initialCount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initialCount));
			}
			_currentCount = initialCount;
		}
		public Task WaitAsync()
		{
			lock (_waiters)
			{
				if (_currentCount > 0)
				{
					--_currentCount;
					return Completed;
				}
				var waiter = new TaskCompletionSource<bool>();
				_waiters.Enqueue(waiter);
				return waiter.Task;
			}
		}

		public void Release()
		{
			TaskCompletionSource<bool> toRelease = null;
			lock (_waiters)
			{
				if (_waiters.Count > 0)
					toRelease = _waiters.Dequeue();
				else
					++_currentCount;
			}
			toRelease?.SetResult(true);
		}
	}
}
