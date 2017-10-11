using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PartialCompilation.Input
{
	public class CtorMultiOverloads
	{
		private readonly Func<IList> _getList;
		private readonly Func<IList, Task> _getListAsync;

		private CtorMultiOverloads(Func<IList> getList, Func<IList, Task> getListAsync)
		{
			_getList = getList;
			_getListAsync = getListAsync;
		}

		private static IList GetList(Func<IList> getList) => null;

		#if TEST

		public static Ctor Create()
		{
			return new CtorMultiOverloads(GetList, GetListAsync);
		}

		#endif

		protected static IList GetList()
		{
			SimpleFile.Read();
			return null;
		}
	}
}
