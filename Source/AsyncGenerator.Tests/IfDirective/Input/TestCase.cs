using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

#if DEBUG
namespace AsyncGenerator.Tests.IfDirective
{
	#if TRACE
	namespace Input
	{
		public class Parent
		{
			public class TestCase
			{
				#if DEBUG

				public void Read()
				{
					SimpleFile.Read();
				}

				public void SyncMethod()
				{

				}
				#elif TRACE
				#else

				public void Sync2Method()
				{

				}

				#endif

			}

			#if TRACE
			public class TestCase2
			{
			}
			#else

			#endif
		}
	}

	#endif

	public class TestCase3
	{
	}

}
#endif