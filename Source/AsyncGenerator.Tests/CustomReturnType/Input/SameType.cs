using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CustomReturnType.Input
{
	public class SameType
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void ReadAsync()
		{
			SimpleFile.ReadAsync().GetAwaiter().GetResult();
		}
	}
}
