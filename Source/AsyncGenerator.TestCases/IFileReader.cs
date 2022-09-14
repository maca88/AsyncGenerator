using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class FileResult
	{
		
	}
	
	public interface IFileReader
	{
		FileResult Read(string path);
	}

	public static class FileReaderExtensions
	{
		public static Task<FileResult> ReadAsync(this IFileReader reader, string path)
		{
			return Task.FromResult(new FileResult());
		}
	}
}