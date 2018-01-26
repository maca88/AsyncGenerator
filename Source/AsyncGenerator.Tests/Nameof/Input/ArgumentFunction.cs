using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Nameof.Input
{
	public class ArgumentFunction
	{
		public Thread Test()
		{
			return new Thread(() =>
			{
				SimpleFile.Write(nameof(Test));
			});
		}
	}
}
