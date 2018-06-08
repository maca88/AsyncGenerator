using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
	public interface ObsoleteInterfaceToken
	{
		IEnumerable<int> Test();
#if OBSOLETE
		[Obsolete]
		Task<IEnumerable<int>> TestAsync(CancellationToken cancellationToken);
#endif
	}

	public class ObsoleteInterfaceTokenDerived : ObsoleteInterfaceToken
	{
		public IEnumerable<int> Test()
		{
			yield return 1;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		public Task<IEnumerable<int>> TestAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(Test());
		}
#endif
	}

	public class ObsoleteInterfaceTokenDerived2 : ObsoleteInterfaceToken
	{
		public IEnumerable<int> Test()
		{
			yield return 2;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		public Task<IEnumerable<int>> TestAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(Test());
		}
#endif
	}
}
