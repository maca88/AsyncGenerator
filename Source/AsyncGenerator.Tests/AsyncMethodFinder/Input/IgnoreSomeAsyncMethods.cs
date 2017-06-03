using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class IgnoreSomeAsyncMethods
	{
		public void Read()
		{
			SimpleFile.Read();
			SimpleFile.Write("");
		}
	}
}
