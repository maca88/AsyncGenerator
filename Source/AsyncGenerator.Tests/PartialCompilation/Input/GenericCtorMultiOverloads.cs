using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PartialCompilation.Input
{
	public class FakeClass<T>
	{
		private readonly Func<IList<T>> _getList;
		private readonly Func<IList<T>, Task> _getListAsync;

		private FakeClass(Func<IList<T>> getList, Func<IList<T>, Task> getListAsync)
		{
			_getList = getList;
			_getListAsync = getListAsync;
		}
	}

	/// <summary>
	/// Github Issue 40
	/// </summary>
	public class GenericCtorMultiOverloads<T>
	{
		private static IList GetList(Func<IList> getList) => null;

		#if TEST

		public static Ctor Create()
		{
			return new FakeClass<T>(GetList, GetListAsync);
		}

		#endif

		private static IList<T> GetList()
		{
			SimpleFile.Read();
			return null;
		}

	}
}
