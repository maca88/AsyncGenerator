using System.IO;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.Partial.SimpleClass
{
	public class TestCase
	{
		public void ReadFile()
		{
			var stream = File.OpenRead("Test");
			stream.Read(new byte[0], 0, 0);
		}

		//public int Sum()
		//{
		//	var sum = 5 + 5;
		//	return sum;
		//}

		//public void ReadFileInNewTask()
		//{
		//	Task.Run(() =>
		//	{
		//		ReadFile();
		//	});
		//}
	}
}
