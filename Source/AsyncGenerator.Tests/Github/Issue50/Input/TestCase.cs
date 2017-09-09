using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue50.Input
{
	public class TestCase : Abstract
	{
		public override void Read()
		{
			SimpleFile.Read();
		}

		public override bool Write(string content)
		{
			return SimpleFile.Write(content);
		}
	}
}
