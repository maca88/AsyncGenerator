using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CustomReturnType.Input
{
	public class TestCase
	{
		public IEnumerable<string> GetData()
		{
			SimpleFile.Read();
			return Enumerable.Empty<string>();
		}

		public IAsyncEnumerable<string> GetDataAsync()
		{
			SimpleFile.ReadAsync();
			return AsyncEnumerable.Empty<string>();
		}

	}
}
