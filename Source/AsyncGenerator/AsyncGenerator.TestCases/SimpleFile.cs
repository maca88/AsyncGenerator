using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
	}
}
