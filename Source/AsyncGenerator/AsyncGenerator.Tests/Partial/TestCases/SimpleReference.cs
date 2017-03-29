using System.IO;

namespace AsyncGenerator.Tests.Partial.TestCases
{
	public class SimpleReference
	{
		public void CallCallReadFile()
		{
			CallReadFile();
		}

		public void CallReadFile()
		{
			ReadFile();
		}

		public void ReadFile()
		{
			var stream = File.OpenRead("");
			stream.Read(new byte[0], 0, 0);
		}

	}
}
