using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public static class StringExtensions
	{
		public static string ReadFile(this string path)
		{
			return File.ReadAllText(path);
		}

		public static async Task<string> ReadFileAsync(this string path, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var stream = File.OpenText(path))
			{
				return await stream.ReadToEndAsync();
			}
		}
	}
}
