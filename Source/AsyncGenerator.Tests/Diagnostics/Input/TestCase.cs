using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Diagnostics.Input
{
	public class TestCase
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Test(out bool success)
		{
			SimpleFile.Read();
			success = true;
		}

		public void Test2(out bool success)
		{
			Read();
			success = true;
		}
	}
}
