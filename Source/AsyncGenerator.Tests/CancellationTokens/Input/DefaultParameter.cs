using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public class DefaultParameter
	{
		public void Read(string test = "Test")
		{
			SimpleFile.Read();
		}

		public void Read2()
		{
			Read();
		}

		public bool Write()
		{
			return SimpleFile.FastWrite();
		}
	}
}
