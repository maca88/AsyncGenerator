using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class ExternalExtensionMethods
	{
		public void External(IFileReader reader)
		{
			reader.Read("test");
		}
	}
}