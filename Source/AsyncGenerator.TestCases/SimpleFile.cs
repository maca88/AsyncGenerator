using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	/// <summary>
	/// Simplify the <see cref="FileStream.Read"/> to have the async counterpart in the same type
	/// </summary>
	public class SimpleFile
	{
		public static void Read()
		{
		}

		public static Task ReadAsync()
		{
			return Task.CompletedTask;
		}

		public static Task ReadAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public static void StreamRead()
		{
		}

		public static Task StreamReadAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public static bool Write(string content)
		{
			return true;
		}

		public static Task<bool> WriteAsync(string content)
		{
			return Task.FromResult(true);
		}

		public static T Write<T>(string content)
		{
			return default(T);
		}

		public static Task<T> WriteAsync<T>(string content)
		{
			return Task.FromResult<T>(default(T));
		}

		public static void Write<T>(T obj, string content)
		{
		}

		public static Task WriteAsync<T>(T obj, string content, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.CompletedTask;
		}


		public static bool FastWrite(string content = "")
		{
			return true;
		}

		public static Task<bool> FastWriteAsync(string content = "", CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.FromResult(true);
		}

		public static void Clear()
		{
			
		}
	}
}
