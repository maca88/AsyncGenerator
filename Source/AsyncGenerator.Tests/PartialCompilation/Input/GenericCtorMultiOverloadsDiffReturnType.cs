using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PartialCompilation.Input
{
	public class FakeClass2<T>
	{
		private readonly Func<IEnumerable<T>> _getList;
		private readonly Func<Task<IEnumerable<T>>> _getListAsync;

		private FakeClass2(Func<IEnumerable<T>> getList, Func<Task<IEnumerable<T>>> getListAsync)
		{
			_getList = getList;
			_getListAsync = getListAsync;
		}
	}

	/// <summary>
	/// Github Issue 40
	/// </summary>
	public class GenericCtorMultiOverloadsDiffReturnType
	{
		#if TEST

		public static Ctor Create()
		{
			return new FakeClass2<T>(List<T>, ListAsync<T>);
		}

		#endif

		private IList<T> List<T>()
		{
			var list = new List<T>();
			List(list);
			return list;
		}

		private void List<T>(IList<T> results)
		{
			SimpleFile.Read();
		}

	}
}
