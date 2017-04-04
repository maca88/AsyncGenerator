using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.Partial.TestCases
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
