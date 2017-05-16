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
	}
}
