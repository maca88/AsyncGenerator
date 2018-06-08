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

		[Obsolete]
		Task<bool> Method4Async(CancellationToken cancellationToken = default(CancellationToken));

		[Obsolete("Obsolete attribute should be copied to concrete implementation")]
		Task<bool> Method5Async(CancellationToken cancellationToken = default(CancellationToken));

		[Obsolete("Obsolete async interface")]
		Task<bool> Method6Async(CancellationToken cancellationToken = default(CancellationToken));
	}
#endif

	public partial interface IInterface
	{
		void Method();

		bool Method2();

		bool Method3();

		bool Method4();

		bool Method5();

		[Obsolete("Obsolete sync interface")]
		bool Method6();
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

		public bool Method4()
		{
			return true;
		}

		public bool Method5()
		{
			return true;
		}

		[Obsolete("Obsolete sync")]
		public bool Method6()
		{
			return true;
		}
	}
}
