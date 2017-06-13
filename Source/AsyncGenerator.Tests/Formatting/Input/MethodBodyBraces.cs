using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class MethodBodyBraces
	{
		public void Test()
		{ SimpleFile.Read(); }

		public void Test2() { SimpleFile.Read(); }

		public void Test3() {}

		T Test<T>() where T : class, new()
		{ return Test2<T>(); }

		public T Test2<T>() where T : class { SimpleFile.Read(); return default(T); }

	}
}
