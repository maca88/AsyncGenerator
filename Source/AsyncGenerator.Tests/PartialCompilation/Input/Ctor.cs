using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PartialCompilation.Input
{
	public class Ctor
	{
		private readonly Func<IList> _getList;
		private readonly Func<IList, Task> _getListAsync;

		protected Ctor(Func<IList> getList, Func<IList, Task> getListAsync)
		{
			_getList = getList;
			_getListAsync = getListAsync;
		}

		#if TEST

		public static Ctor Create()
		{
			return new Ctor(GetList, GetListAsync);
		}

		#endif

		protected static IList GetList()
		{
			SimpleFile.Read();
			return null;
		}
	}
}
