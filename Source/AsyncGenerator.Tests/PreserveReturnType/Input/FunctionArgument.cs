using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.PreserveReturnType.Input
{
	public class FunctionArgument
	{
		public void Test()
		{
			Assert.DoesNotThrow(SimpleFile.Read);
		}
	}
}
