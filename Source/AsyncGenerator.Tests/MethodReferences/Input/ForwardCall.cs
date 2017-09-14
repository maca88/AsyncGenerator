using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MethodReferences.Input
{
	public class ForwardCall
	{
		[Custom]
		public void Test()
		{
			SimpleFile.Read();
		}

		[Custom]
		public void ForwardTest()
		{
			Test();
		}
	}
}
