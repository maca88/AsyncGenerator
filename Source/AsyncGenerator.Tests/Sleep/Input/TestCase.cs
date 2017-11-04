using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Sleep.Input
{
	public class TestCase
	{
		/// <summary>
		/// <see cref="Thread.Sleep(int)"/>
		/// <see cref="System.Threading.Thread.Sleep(TimeSpan)"/>
		/// </summary>
		public void Sleep()
		{
			Thread.Sleep(10);
			System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(10));

			var name = nameof(Thread.Sleep);
			Assert.AreEqual(nameof(System.Threading.Thread.Sleep), name);

			Runner.RunWithIntParameter(Thread.Sleep);
			Runner.RunWithIntParameter(System.Threading.Thread.Sleep);
		}

		public void Sleep2()
		{
			Thread.Sleep(10);
		}
	}
}
