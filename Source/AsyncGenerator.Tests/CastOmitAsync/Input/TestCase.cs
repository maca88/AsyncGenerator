using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CastOmitAsync.Input
{
	public class TestCase
	{
		public long LongCastReturn()
		{
			return ReadFileInt();
		}

		public IEnumerable<string> EnumerableCastReturn()
		{
			return ReadFiles();
		}

		public List<string> NoCastReturn()
		{
			return ReadFiles();
		}

		public Task NoCastReturnTask()
		{
			return Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			});
		}

		public int ReadFileInt()
		{
			SimpleFile.Read();
			return 1;
		}

		public List<string> ReadFiles()
		{
			SimpleFile.Read();
			return new List<string>();
		}
	}
}
