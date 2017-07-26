using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Plugins.Input
{
	public class NestedEmptyRegions
	{
		#region Async

		#region Async2

		#region AsyncNested
		#endregion AsyncNested

		public void Test()
		{
			SimpleFile.Read();
		}

		#endregion Async2

		#endregion Async

		/// <summary>
		/// Nested class
		/// </summary>
		public class MyClass
		{
			#region MyClassAsync

			public void NestedTest()
			{
				SimpleFile.Read();
			}

			#endregion MyClassAsync

			#region MyClassSync

			#endregion MyClassSync
		}

		#region Sync

		#region SyncNested

		#endregion SyncNested

		#endregion Sync
	}
}
