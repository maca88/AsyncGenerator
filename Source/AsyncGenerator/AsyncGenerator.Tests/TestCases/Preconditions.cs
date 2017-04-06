using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public static class Requires
	{
		public static void IsNotNull(object value, string name)
		{
			if (value == null)
				throw new ArgumentNullException(name);
		}
	}

	public class Preconditions
	{
		public void ReadFile(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}
			SimpleFile.Read();
		}

		public int Divide(int a, int b)
		{
			if (b == 0) throw new InvalidOperationException();
			SimpleFile.Read();
			return a/b;
		}

		public int DivideShort(short a, short b)
		{
			Requires.IsNotNull(b, nameof(b));
			SimpleFile.Read();
			return a/b;
		}

	}
}
