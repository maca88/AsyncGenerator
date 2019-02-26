using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.LocalVariables.Input
{
	public class InternalMethod
	{
		public void Test()
		{
			Action test = Read;
			test();
		}

		private void Read()
		{
			SimpleFile.Read();
		}
	}
}
