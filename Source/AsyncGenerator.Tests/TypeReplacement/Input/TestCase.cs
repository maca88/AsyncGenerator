using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TypeReplacement.Input
{
	public class TestCase
	{
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
		private readonly ReaderWriterLockSlim _lock2;

		public TestCase()
		{
			_lock2 = new ReaderWriterLockSlim();
		}

		public void Read()
		{
			_lock.EnterReadLock();

			try
			{
				SimpleFile.Read();
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}

		public void Clear()
		{
			_lock.EnterWriteLock();
			try
			{
				SimpleFile.Clear();
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		public void Write()
		{
			_lock2.EnterWriteLock();
			try
			{
				SimpleFile.Write("");
			}
			finally
			{
				_lock2.ExitWriteLock();
			}
		}

		public void ReadWrite()
		{
			_lock2.EnterUpgradeableReadLock();
			try
			{
				_lock2.EnterWriteLock();
				try
				{
					SimpleFile.Write("");
				}
				finally
				{
					_lock2.ExitWriteLock();
				}
			}
			finally
			{
				_lock2.ExitUpgradeableReadLock();
			}
		}

	}
}
