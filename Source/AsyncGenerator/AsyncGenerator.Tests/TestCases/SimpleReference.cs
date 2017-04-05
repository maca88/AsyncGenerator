using System.IO;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
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
			SimpleFile.Read();
		}

	}
}
