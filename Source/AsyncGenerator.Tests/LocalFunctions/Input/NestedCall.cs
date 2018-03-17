using AsyncGenerator.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class NestedCall
	{
		public void Test()
		{
			LocalRead();
			void LocalRead()
			{
				Read();
			}
		}

		public void Read()
		{
			SimpleFile.Read();
		}
	}
}
