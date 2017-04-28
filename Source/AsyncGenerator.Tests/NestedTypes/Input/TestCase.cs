using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NestedTypes.Input
{
	public class TestCase
	{
		public class Child
		{
			public void Read()
			{
				SimpleFile.Read();
			}
		}

		public void VoidMethod()
		{
			
		}
	}
}
