using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.LocalVariables.Input
{
	public class ExternalAction
	{
		public void Test()
		{
			Action test = SimpleFile.Read;
			test = null;
			test = SimpleFile.Read;
			test();
		}
	}
}
