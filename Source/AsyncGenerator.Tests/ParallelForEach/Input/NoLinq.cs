using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelForEach.Input
{
	public class NoLinq
	{
		public void Read()
		{
			Parallel.ForEach(new []{ "" }, i =>
			{
				SimpleFile.Read();
			});
		}
	}
}
