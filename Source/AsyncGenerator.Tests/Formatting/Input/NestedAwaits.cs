using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class NestedAwaits
	{
		public class Reader
		{
			public Reader Read()
			{
				SimpleFile.Read();
				return this;
			}
		}


		public void Test()
		{
			var reader = new Reader();
			reader.Read().Read();
		}
	}
}
