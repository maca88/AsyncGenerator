using System;
using System.IO;
using System.Text;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class Invocation
	{
		public void Test()
		{
			SimpleFile.Clear();
		}

		public bool Test2()
		{
			return SimpleFile.IsBusy();
		}
	}
}
