using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PreserveReturnType.Input
{
	public class LastStatement
	{
		private void Read()
		{
			SimpleFile.Read();
		}

		public void Test()
		{
			Runner.RunWithIntParameter(i =>
			{
				SimpleFile.Read();
			});

			Read();
		}
	}
}
