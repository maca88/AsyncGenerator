using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class ExtensionMethods
	{
		public void External()
		{
			"test".ReadFile();
		}

		public void InternalGeneric()
		{
			var item = Query<string>().Delete();
		}

		public IQueryable<T> Query<T>()
		{
			return null;
		}
	}
}
