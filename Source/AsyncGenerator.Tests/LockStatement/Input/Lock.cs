using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.LockStatement.Input
{
	public class Lock
	{
		private readonly object _lock = new object();
		private static readonly object StaticLock = new object();

		public void Test()
		{
			lock (typeof(Lock))
			{
				SimpleFile.Read();
			}

			lock (typeof(Lock))
			{
				Console.WriteLine("Test");
			}

			lock (typeof(Lock))
				SimpleFile.Read();

			lock (_lock)
			{
				lock (typeof(Lock))
				{
					SimpleFile.Read();
				}

				lock (StaticLock)
				{
					SimpleFile.Read();
				}
			}

			lock (_lock)
				SimpleFile.Read();
		}

		public void Test2()
		{
			lock (typeof(Lock))
			{
				SimpleFile.Read();

				lock (StaticLock)
				{
					SimpleFile.Read();
				}
			}

			lock (_lock)
				SimpleFile.Read();
		}
	}
}
