using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class TestCase
	{
		private readonly NestedClass _nestedClass;

		public TestCase()
		{
			_nestedClass = new NestedClass();
		}

		public class NestedClass
		{
			public void Nop() { }
		}

		public void Read()
		{
			DoSomething();
			SimpleFile.Read();
		}

		public void DoSomething()
		{
			_nestedClass.Nop();
		}

	}
}
