using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SimpleCircularCall.Input
{
	public class TestCase
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
