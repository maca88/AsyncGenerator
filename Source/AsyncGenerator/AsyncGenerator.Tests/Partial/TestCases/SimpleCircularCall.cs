using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.Partial.TestCases
{
	public class SimpleCircularCall
	{
		public void Method1()
		{
			Method2();
		}

		public void Method2()
		{
			if (ReadFile() > 0)
			{
				Method1();
			}
		}


		public int ReadFile()
		{
			var stream = File.OpenRead("");
			return stream.Read(new byte[0], 0, 0);
		}
	}
}
