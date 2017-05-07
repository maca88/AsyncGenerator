using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CallForwarding.Input
{
	public interface IInterface
	{
		void SyncRead();

		bool SyncWrite();
	}

	public class TestCase : IInterface
	{
		public void SyncMethod()
		{
			for (var i = 0; i < 100; i++)
			{
				Console.WriteLine("Hello!");
			}
		}

		public void SyncMethod2()
		{
			for (var i = 0; i < 100; i++)
			{
				Console.WriteLine("Hello!");
			}
		}

		public bool SyncMethod3(string content)
		{
			if (content == null)
			{
				throw new ArgumentNullException();
			}
			for (var i = 0; i < 100; i++)
			{
				Console.WriteLine("Hello!");
			}
			return true;
		}

		void IInterface.SyncRead()
		{
			for (var i = 0; i < 100; i++)
			{
				Console.WriteLine("Hello!");
			}
		}

		bool IInterface.SyncWrite()
		{
			for (var i = 0; i < 100; i++)
			{
				Console.WriteLine("Hello!");
			}
			return false;
		}

		public void Read()
		{
			SimpleFile.Read();
		}
	}
}
