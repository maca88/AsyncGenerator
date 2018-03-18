using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ExceptionHandling.Input
{
	public class NoCatchMethod
	{
		public void Test()
		{
			SimpleFile.Clear();
			SimpleFile.Read();
		}

		public void Test2()
		{
			SimpleFile.Clear();
			SimpleFile.Read();
		}
	}
}
