using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	#if TEST
	public class MissingMembers : IExternalInterface
	{
		public class NestedClass : IExternalInterface
		{
			public void Method()
			{
			}

			public bool Method2()
			{
				return SimpleFile.Write("");
			}

			public bool Method3()
			{
				return false;
			}
		}

		public void Method()
		{
			SimpleFile.Read();
		}

		public bool Method2()
		{
			return SimpleFile.Write("");
		}

		public bool Method3()
		{
			return Method2();
		}

		public void Read()
		{
			Method();
		}
	}
	#endif
}
