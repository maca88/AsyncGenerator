using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.IfDirective.Input
{
	public class VoidReturnType
	{
		public void Test()
		{
#if TEST
			SimpleFile.Read();
#else
			SimpleFile.Read();
#endif
		}

		public void Test2()
		{
#if ASYNC
			SimpleFile.Read();
#else
			SimpleFile.Read();
#endif
		}

		public void Test3()
		{
#if TEST
			SimpleFile.Read();
#elif ASYNC
			SimpleFile.Read();
#else
			SimpleFile.Read();
#endif
		}
	}
}
