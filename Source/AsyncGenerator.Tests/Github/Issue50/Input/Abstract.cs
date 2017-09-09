using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue50.Input
{
	public abstract class Abstract : IInterface
	{
		public virtual void Read()
		{
			SimpleFile.Read();
		}

		public abstract bool Write(string content);
	}
}
