using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.IfDirective.Input
{
	public class TryCatch
	{
		public int Test()
		{
#if TEST
			var result = new Random();
			return result.Next() + 1;
#else
			var result = new Random();
			return result.Next();
#endif
		}
	}
}
