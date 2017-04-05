using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

public delegate void Test();

public class TestCase2
{
}

namespace AsyncGenerator.Tests.TestCases
{
	namespace MyNamespace
	{
		public class TestCase2
		{
		}
	}
}

namespace AsyncGenerator.Tests.TestCases
{
	namespace MyNamespace
	{
		public class TestCase
		{
			public void ReadFile()
			{
				var stream = File.OpenRead("");
				stream.Read(new byte[0], 0, 0);
			}

			public long GetReadFileResult()
			{
				var stream = File.OpenRead("");
				return (long)stream.Read(new byte[0], 0, 0);
			}

			public void ReadFile2()
			{
				var stream = File.OpenRead("");
				ReadStream(stream.Read);
			}

			private void ReadStream(Func<byte[], int, int, int> readStremFn)
			{
				readStremFn(new byte[0], 0, 0);
			}

			//public int Sum()
			//{
			//	var sum = 5 + 5;
			//	return sum;
			//}

			public delegate void Del(out string message);

			public void ReadFileInNewTask()
			{
				Action ddd = delegate { };

				var dd4 = new Del((out string message) =>
				{
					message = "";
				});

				Func<Task, object> dd2 = task =>
				{
					return task.ConfigureAwait(false);
				};
				Task.Run(() =>
				{
					ReadFile();
				});
			}

			delegate void Test();

			public void ReadFileInNewAsyncTask()
			{
				Task.Run(async () =>
				{
					await Task.Yield();
					ReadFile();
				});
			}
		}
	}

	
}
