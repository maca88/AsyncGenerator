using System.IO;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class SimpleCircularCall
	{
		public void Method1()
		{
			Method2();
		}

		public void Method2()
		{
			if (!ReadFile())
			{
				Method1();
			}
		}

		public bool ReadFile()
		{
			SimpleFile.Read();
			return true;
		}
	}
}
