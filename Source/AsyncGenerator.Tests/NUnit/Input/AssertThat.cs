using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	public class AssertThat
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

			Assert.That(() =>
			{
				Console.WriteLine("");
			}, Throws.Nothing);

			var result = false;
			Assert.That(() => result = SimpleFile.Write(""), Throws.Nothing);
			Assert.IsTrue(result);

		}

		public void That()
		{
			Assert.That(() => Console.WriteLine(""), Throws.Nothing);
			Assert.That(() => SimpleFile.Clear(), Throws.Nothing);
			Assert.That(() => Read(), Throws.Nothing);
		}

		public void ThatWithReturn()
		{
			Assert.That(() => Console.Read(), Throws.Nothing);
			Assert.That(() => SimpleFile.IsBusy(), Throws.Nothing);
			Assert.That(() => Write(), Throws.Nothing);
		}

		private void Read()
		{
			
		}

		private bool Write()
		{
			return true;
		}
	}
}
