using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelFor.Input
{
	public class TestCase
	{
		public void Read()
		{
			Parallel.For(1, 100, i =>
			{
				SimpleFile.Read();
			});
			Parallel.For(1, 100, Read);
			Parallel.For(1, 100, i =>
			{
				SimpleFile.Read();
				Parallel.For(1, 100, j =>
				{
					SimpleFile.Read();
				});
			});
			Parallel.For(1, 100, s => Console.WriteLine(s));

			// Not supported cases
			Parallel.For(1L, 100L, Read);
			Parallel.For(1L, 100L, i =>
			{
				SimpleFile.Read();
			});
			Parallel.For(1, 100, (i, state) =>
			{
				SimpleFile.Read();
			});
			Parallel.For(1L, 100L, (i, state) =>
			{
				SimpleFile.Read();
			});
			Parallel.For(1, 100, (i, state) =>
			{
				SimpleFile.Read();
				Parallel.For(1L, 100L, j =>
				{
					SimpleFile.Read();
				});
			});
			var completed = Parallel.For(1, 100, Read).IsCompleted;
			var result = Parallel.For(1, 100, Read);
		}

		private void Read(int i)
		{
			SimpleFile.Read();
		}

		private void Read(long i)
		{
			SimpleFile.Read();
		}
	}
}
