using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class SubNestedClass
	{
		public void Read()
		{
			var nested = NestedFactory.Create();
			nested.DoSomething();
			SimpleFile.Read();
		}

		public class NestedFactory
		{
			public static Nested Create()
			{
				return new Nested();
			}
		}

		public class Nested
		{
			public void DoSomething()
			{
				var sub = new SubNested();
				sub.Do();
			}

			private class SubNested
			{
				public void Do()
				{
					
				}
			}
		}
	}
}
