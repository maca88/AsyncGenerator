using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SearchInheritedTypes.Input
{
	public class Interface
	{
		public void Test(IDatabase database)
		{
			database.Save();
		}
	}
}
