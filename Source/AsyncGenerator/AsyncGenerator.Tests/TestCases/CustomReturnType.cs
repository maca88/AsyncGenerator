using System.Collections.Generic;
using System.Linq;

namespace AsyncGenerator.Tests.TestCases
{
	public class CustomReturnType
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
