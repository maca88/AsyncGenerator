#if NETCOREAPP2_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncReturnType.Input
{
	public class TestCase
	{
		public void Read()
		{
			SimpleFile.ReadValue();
		}

		public bool Write(string content)
		{
			return SimpleFile.WriteValue(content);
		}
	}
}
#endif
