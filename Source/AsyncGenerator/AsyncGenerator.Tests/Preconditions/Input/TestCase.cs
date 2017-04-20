using System;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Preconditions.Input
{
	public static class Requires
	{
		public static void IsNotNull(object value, string name)
		{
			if (value == null)
				throw new ArgumentNullException(name);
		}
	}

	public class TestCase
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
