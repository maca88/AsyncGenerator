using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MethodGeneration.Input
{
	public class TestCase
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Read2()
		{
			Read();
		}
	}
}
