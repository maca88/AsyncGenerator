using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class AlreadyAsync
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Read2()
		{
		}

		public Task Read2Async()
		{
			return SimpleFile.ReadAsync();
		}

		public Task Read3Async()
		{
			return SimpleFile.ReadAsync();
		}
	}
}
