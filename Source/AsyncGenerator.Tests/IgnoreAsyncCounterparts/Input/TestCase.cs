using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.IgnoreAsyncCounterparts.Input
{
	public class TestCase
	{
		public void Test()
		{
			Read();
			SimpleFile.Read();
		}

		public void Read()
		{
			SimpleFile.Read();
		}

		public Task ReadAsync()
		{
			return SimpleFile.ReadAsync();
		}
	}
}
