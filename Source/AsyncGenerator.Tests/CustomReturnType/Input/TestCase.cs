using System.Collections.Generic;
using System.Linq;

namespace AsyncGenerator.Tests.CustomReturnType.Input
{
	public class TestCase
	{
		public IEnumerable<string> GetData()
		{
			return Enumerable.Empty<string>();
		}

		public IAsyncEnumerable<string> GetDataAsync()
		{
			return AsyncEnumerable.Empty<string>();
		}

	}
}
