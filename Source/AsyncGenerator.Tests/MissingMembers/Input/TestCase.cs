using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
#if TEST
	public partial interface IInterface
	{
		Task MethodAsync();

		Task<bool> Method2Async(CancellationToken cancellationToken);

		Task<bool> Method3Async(CancellationToken cancellationToken = default(CancellationToken));
	}
#endif

	public partial interface IInterface
	{
		void Method();

		bool Method2();

		bool Method3();
	}

	public class TestCase : IInterface
	{
		public void Method()
		{
		}

		public bool Method2()
		{
			return true;
		}

		public bool Method3()
		{
			return true;
		}
	}
}
