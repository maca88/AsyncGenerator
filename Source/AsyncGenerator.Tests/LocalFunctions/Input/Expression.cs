using AsyncGenerator.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class Expression
	{
		public void Test()
		{
			void Read() => SimpleFile.Read();
			Read();
		}
	}
}
