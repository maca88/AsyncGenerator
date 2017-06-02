using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.AsyncMethodFinder.Input
{
	public class NUnitAssertThat
	{
		public void Test()
		{
			Assert.That(1, Is.EqualTo(1));

			Assert.That(() =>
			{
				SimpleFile.Read();
				return true;
			}, Is.EqualTo(true));

			Assert.That(() =>
			{
				SimpleFile.Read();
				return true;
			});

			Assert.That(() =>
			{
				SimpleFile.Read();
			}, Throws.Nothing);

			var result = false;
			Assert.That(() => result = SimpleFile.Write(""), Throws.Nothing);
			Assert.IsTrue(result);

		}
	}
}
