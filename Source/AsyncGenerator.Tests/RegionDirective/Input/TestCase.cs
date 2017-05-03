#region MyRegion

// This is a test region

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

#region Namespace
namespace AsyncGenerator.Tests.RegionDirective
{
	#region Input
	namespace Input
	{
		public class Parent
		{
			public class TestCase
			{
				#region Async methods

				public void Read()
				{
					SimpleFile.Read();
				}

				#endregion Async methods

				#region Sync methods region

				public void SyncMethod()
				{

				}

				public void Sync2Method()
				{

				}

				#endregion Sync methods region

			}

			#region Empty 

			public class TestCase2
			{
			}

			#endregion Empty
		}
	}

	#endregion Input

	public class TestCase3
	{
	}

	#region Nested type: TestCase4

	/// <summary>
	/// Test class
	/// </summary>
	public abstract class TestCase4
	{
		public System.Type Property1 { get; set; }

		#region Members

		public int Property2 { get; set; }

		public abstract long Property3 { get; }

		public abstract bool Property4 { get; }

		public void Read()
		{
			SimpleFile.Read();
		}

		#endregion

		protected virtual object SyncMethod(long value)
		{
			return null;
		}
	}

	#endregion

}
#endregion Namespace