using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Partial.SimpleClass
{
	public class TestCase
	{
		public void ShouldBeAsync()
		{
			if (new Random().Next(0, 5) <= 10) return;
			var stream = File.OpenRead("Test");
			stream.Read(new byte[0], 0, 0);
		}

		public void ShouldBeSync()
		{
			var sum = 5 + 5;
			Assert.AreEqual(10, sum);
		}
	}
}
