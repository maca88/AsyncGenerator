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
	public abstract partial class AbstractBase
	{
		public abstract Task MethodAsync();

		protected abstract Task<bool> Method2Async(CancellationToken cancellationToken);

		internal abstract Task<bool> Method3Async(CancellationToken cancellationToken = default(CancellationToken));

		[Obsolete]
		public abstract Task<bool> Method4Async(CancellationToken cancellationToken = default(CancellationToken));

		[Obsolete("Obsolete attribute should be copied to concrete implementation")]
		public abstract Task<bool> Method5Async(CancellationToken cancellationToken = default(CancellationToken));

		[Obsolete("Obsolete async base")]
		public abstract Task<bool> Method6Async(CancellationToken cancellationToken = default(CancellationToken));
	}
#endif

	public abstract partial class AbstractBase
	{
		public abstract void Method();

		protected abstract bool Method2();

		internal abstract bool Method3();

		public abstract bool Method4();

		public abstract bool Method5();

		[Obsolete("Obsolete sync base")]
		public abstract bool Method6();
	}

	public class TestAbstract : AbstractBase
	{
		public override void Method()
		{
		}

		protected override bool Method2()
		{
			return true;
		}

		internal override bool Method3()
		{
			return true;
		}

		public override bool Method4()
		{
			return true;
		}

		public override bool Method5()
		{
			return true;
		}

		[Obsolete("Obsolete sync")]
		public override bool Method6()
		{
			return true;
		}
	}
}
