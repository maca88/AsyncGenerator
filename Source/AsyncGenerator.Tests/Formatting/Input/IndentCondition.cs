using System;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class IndentCondition
	{
		public void Test(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			SimpleFile.Read();
			value = value == "null" ? "test" : value;
			value = value != "test"
				? "val" : value;
			value = value != "test"
				? "val"
				: value;
		}
	}
}
