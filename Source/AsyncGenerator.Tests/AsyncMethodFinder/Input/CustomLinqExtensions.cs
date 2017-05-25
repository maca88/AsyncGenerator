using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class CustomLinqExtensions
	{

		public IQueryable<T> Query<T>()
		{
			return null;
		}

		public void DoQuery()
		{
			var totalCount = Query<string>().Count();
			var item = Query<string>().FirstOrDefault(o => o.Length == totalCount);
		}

		public void DoAverage()
		{
			var item = Query<string>().Average(o => o.Length);
		}
	}
}
