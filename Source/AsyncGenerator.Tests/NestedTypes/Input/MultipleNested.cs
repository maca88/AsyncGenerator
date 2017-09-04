using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NestedTypes.Input
{
	public class MultipleNested
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public class Nested1
		{
			public bool Property { get; set; }

			public void DoSomething()
			{
				
			}
		}

		public class Nested2
		{
			public bool Property { get; set; }

			public void DoSomething()
			{

			}
		}
	}
}
