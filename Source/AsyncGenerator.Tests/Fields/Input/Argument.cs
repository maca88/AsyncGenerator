using System;
using System.Threading;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Fields.Input
{
	public class Argument
	{
		private readonly string _test = "test";

		public void Write()
		{
			var result = SimpleFile.Write(_test);
		}
	}
}
