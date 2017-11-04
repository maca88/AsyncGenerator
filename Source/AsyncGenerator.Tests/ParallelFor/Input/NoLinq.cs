using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelFor.Input
{
	public class NoLinq
	{
		public void Read()
		{
			Parallel.For(1, 100, i =>
			{
				SimpleFile.Read();
			});
		}
	}
}
