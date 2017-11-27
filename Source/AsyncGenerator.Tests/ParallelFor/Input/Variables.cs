using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelFor.Input
{
	public class Variables
	{
		public void Read()
		{
			var start = 5;
			var end = 10;
			Parallel.For(start, end, i =>
			{
				SimpleFile.Read();
			});
		}

		public void Read2(int start, int end)
		{
			Parallel.For(start, end, i =>
			{
				SimpleFile.Read();
			});
		}
	}
}
