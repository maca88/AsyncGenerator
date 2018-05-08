using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Nameof.Input
{
	public class SelfReference
	{
		public void Write()
		{
			SimpleFile.Write(nameof(Write));
		}
	}
}
