using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.LocalVariables.Input
{
	public class ExternalFunc
	{
		public void Test()
		{
			Func<string, bool> test = SimpleFile.Write;
			test("");
		}
	}
}
