using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
	public interface ObsoleteInterfaceAbstractToken
	{
		/// <summary>
		/// Test
		/// </summary>
		/// <returns></returns>
		IEnumerable<int> Test();
#if OBSOLETE
		// Test2
		/// <summary>
		/// Test
		/// </summary>
		/// <param name="cancellationToken">Token</param>
		/// <returns></returns>
		// Test1
		// ttttt
		[Obsolete]
		Task<IEnumerable<int>> TestAsync(CancellationToken cancellationToken);
#endif
	}

	public abstract class ObsoleteInterfaceAbstractTokenDerived : ObsoleteInterfaceAbstractToken
	{
		public abstract IEnumerable<int> Test();
	}

	public class ObsoleteInterfaceAbstractTokenDerived2 : ObsoleteInterfaceAbstractTokenDerived
	{
		public override IEnumerable<int> Test()
		{
			yield return 2;
		}
	}
}
