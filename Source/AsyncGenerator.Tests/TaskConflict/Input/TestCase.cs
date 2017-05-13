using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TaskConflict.Input
{
	public class TestCase
	{
		public void Method()
		{
			SimpleFile.Read();
		}

		public void WriteLine()
		{
			System.Console.WriteLine();
		}

		public int Read()
		{
			return System.Console.Read();
		}
	}
}
