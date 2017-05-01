using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Yields.Input
{
	public class TestCase
	{
		public IEnumerable<bool> GetBools(bool value)
		{
			yield return SimpleFile.Write("");
			if (!value)
			{
				yield break;
			}
			yield return true;
		}

		public IEnumerable<int> GetInts(bool value)
		{
			if (!value)
			{
				yield break;
			}
			SimpleFile.Read();
		}

		public IEnumerable<long> GetLongs(bool value)
		{
			if (!value)
			{
				yield break;
			}
			yield return 1;
			yield return 2;
		}
	}
}
