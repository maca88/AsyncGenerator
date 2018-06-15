using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SearchInheritedTypes.Input
{
	public class AbstractClass
	{
		public int Test(SqlCommand command)
		{
			return command.ExecuteNonQuery();
		}
	}
}
