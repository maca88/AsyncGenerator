using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	public class NestedAssertThat
	{
		public void Test()
		{
			Throws<NestedAssertThat>();
		}

		private void Throws<T>()
		{
			Assert.Throws<Exception>(
				() => new NestedClass().Build(), $"Test{typeof(T)}");
		}

		private class NestedClass
		{
			public void Build()
			{

			}
		}
	}
}
