using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public class Params
	{
		public void Read(params string[] param)
		{
			SimpleFile.Read();
		}
	}
}
