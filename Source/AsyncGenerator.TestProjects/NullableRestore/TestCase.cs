using System.IO;

namespace NullableRestore
{
	public class TestCase
	{
		public string? ReadFile()
		{
			var stream = File.OpenRead("");
			stream.Read(new byte[] { }, 0, 100);
			stream.Dispose();
			return null;
		}
	}
}