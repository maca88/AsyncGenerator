using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class ElementAccess
	{
		private readonly List<string> _list = new List<string> {"Test"};

		public string Test()
		{
			return _list[10];
		}
	}
}
