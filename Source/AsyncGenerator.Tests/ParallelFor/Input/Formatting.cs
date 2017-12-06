using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelFor.Input
{
	public class Formatting
	{
		public void Test()
		{
			Parallel
				.For
				(
					1,
					12,
					i =>
					{
						SimpleFile.Read();
					}
				);
		}
	}
}
