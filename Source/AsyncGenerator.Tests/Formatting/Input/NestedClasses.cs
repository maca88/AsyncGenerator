using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class NestedClasses
	{
		public void Test()
		{
			new Nested().Read();
			new Nested2().Read();
			Nested3.Read();
		}

		class Nested
		{
			public void Read()
			{
				SimpleFile.Read();
			}
		}

		sealed class Nested2
		{
			public void Read()
			{
				SimpleFile.Read();
			}
		}

		static class Nested3
		{
			public static void Read()
			{
				SimpleFile.Read();
			}
		}
	}
}
