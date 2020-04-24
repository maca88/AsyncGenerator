using System;
using System.IO;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class IndentSwitch
	{
		public void Test(string value, IDisposable item)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			SimpleFile.Read();
			switch (item)
			{
				case BinaryReader br:
					value = "test";
					break;
				case Stream _:
					value = "test2";
					break;
				default:
					throw new InvalidOperationException();
			}

			switch (value)
			{
				case "test":
					return;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
