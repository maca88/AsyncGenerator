using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NestedNamespaces.Input
{
	public class TestCase
	{
		public class Child
		{
			public void Read()
			{
				SimpleFile.Read();
			}
		}

		public void VoidMethod()
		{

		}
	}

	namespace Inner
	{
		public class TestCase
		{
			public class Child
			{
				public void Read()
				{
					SimpleFile.Read();
				}
			}

			public void VoidMethod()
			{

			}
		}
	}

	namespace Empty
	{
		public class TestCase
		{
			public void VoidMethod()
			{

			}
		}
	}
}