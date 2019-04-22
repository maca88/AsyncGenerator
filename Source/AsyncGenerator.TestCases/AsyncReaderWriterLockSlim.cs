using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class AsyncReaderWriterLockSlim
	{
		public Task EnterReadLockAsync()
		{
			return Task.CompletedTask;
		}

		public void ExitReadLock()
		{
		}

		public Task EnterWriteLockAsync()
		{
			return Task.CompletedTask;
		}

		public void ExitWriteLock()
		{
		}

		public Task EnterUpgradeableReadLockAsync()
		{
			return Task.CompletedTask;
		}

		public void ExitUpgradeableReadLock()
		{
		}
	}
}
