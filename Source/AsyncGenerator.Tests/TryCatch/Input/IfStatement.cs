using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class IfStatement
	{
		public bool Success;

		public void Test()
		{
			if (Success)
				SimpleFile.Read();
		}
	}
}
