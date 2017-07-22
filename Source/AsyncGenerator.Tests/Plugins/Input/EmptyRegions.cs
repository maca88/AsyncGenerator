using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Plugins.Input
{
	public class EmptyRegions
	{
		#region Test

		#endregion Test

		#region MyRegion

		#endregion MyRegion

		#region Async

		public void Test()
		{
			SimpleFile.Read();
		}

		#endregion Async
		#region Empty1

		#endregion
		#region Empty2

		#endregion

		#region Sync

		#region Nested

		public void Test2()
		{
		}

		#endregion

		public void Test3()
		{
		}

		#endregion Sync
	}
}
