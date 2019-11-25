using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
	public interface ObsoleteInterface
	{
		IEnumerable<int> Test();
#if OBSOLETE
		[Obsolete]
		Task<IEnumerable<int>> TestAsync();
#endif
	}

	public class ObsoleteInterfaceDerived : ObsoleteInterface
	{
		public IEnumerable<int> Test()
		{
			yield return 1;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		public Task<IEnumerable<int>> TestAsync()
		{
			return Task.FromResult(Test());
		}
#endif
	}

	public class ObsoleteInterfaceDerived2 : ObsoleteInterface
	{
		public IEnumerable<int> Test()
		{
			yield return 2;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		public Task<IEnumerable<int>> TestAsync()
		{
			return Task.FromResult(Test());
		}
#endif
	}

	public class CallObsoleteInterface
	{
		public IEnumerable<int> Call(ObsoleteInterface obsolete)
		{
			if (obsolete != null)
			{
				return obsolete.Test();
			}

			return obsolete.Test();
		}
	}
}
