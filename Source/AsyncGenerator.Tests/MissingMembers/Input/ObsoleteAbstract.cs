using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using AsyncGenerator.Tests.LocalFunctions.Input;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
	public abstract class ObsoleteAbstract
	{
		public abstract IEnumerable<int> Test();
#if OBSOLETE
		[Obsolete]
		protected internal abstract Task<IEnumerable<int>> TestAsync();
#endif
	}

	public class ObsoleteAbstractDerived : ObsoleteAbstract
	{
		public override IEnumerable<int> Test()
		{
			yield return 1;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		protected internal override Task<IEnumerable<int>> TestAsync()
		{
			return Task.FromResult(Test());
		}
#endif
	}

	public class ObsoleteAbstractDerived2 : ObsoleteAbstract
	{
		public override IEnumerable<int> Test()
		{
			yield return 2;
		}
#if OBSOLETE && OVERRIDE
		[Obsolete]
		protected internal override Task<IEnumerable<int>> TestAsync()
		{
			return Task.FromResult(Test());
		}
#endif
	}
}
