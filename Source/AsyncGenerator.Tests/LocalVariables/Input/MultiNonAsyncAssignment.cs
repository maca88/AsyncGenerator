using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.LocalVariables.Input
{
	public class MultiNonAsyncAssignment
	{
		public void Test(bool value)
		{
			Action test = Read;
			if (value)
			{
				test = SimpleFile.Read;
			}

			test();
		}

		private void Read()
		{
		}
	}
}
