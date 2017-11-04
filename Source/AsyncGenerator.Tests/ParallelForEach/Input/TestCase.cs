using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ParallelForEach.Input
{
	public class TestCase
	{
		public void Read()
		{
			Parallel.ForEach(Enumerable.Range(1, 100), i =>
			{
				SimpleFile.Read();
			});
			Parallel.ForEach(Enumerable.Range(1, 100), Read);
			Parallel.ForEach(GetStringList(), i =>
			{
				SimpleFile.Read();
			});
			Parallel.ForEach(GetStringList(), Read);
			Parallel.ForEach(Results, Read);
			Parallel.ForEach(Enumerable.Empty<string>(), Read);
			Parallel.ForEach(GetStringList(), i =>
			{
				SimpleFile.Read();
				Parallel.ForEach(GetStringList(), j =>
				{
					SimpleFile.Read();
				});
			});
			Parallel.ForEach(Enumerable.Empty<string>(), s => Console.WriteLine(s));
			Parallel.ForEach(Enumerable.Empty<string>(), File.Delete);

			// Not supported cases
			Parallel.ForEach(Enumerable.Range(1, 100), (i, state) =>
			{
				SimpleFile.Read();
			});
			Parallel.ForEach(Enumerable.Range(1, 100), (i, state, arg3) =>
			{
				SimpleFile.Read();
			});
			Parallel.ForEach(GetStringList(), (i, state, arg3) =>
			{
				SimpleFile.Read();
				Parallel.ForEach(GetStringList(), j =>
				{
					SimpleFile.Read();
				});
			});
			var completed = Parallel.ForEach(Results, Read).IsCompleted;
			var result = Parallel.ForEach(Results, Read);
		}

		private void Read(int i)
		{
			SimpleFile.Read();
		}

		private void Read(string i)
		{
			SimpleFile.Read();
		}

		private IEnumerable<string> Results { get; } = Enumerable.Empty<string>();

		private IEnumerable<string> GetStringList()
		{
			yield break;
		}
	}
}
