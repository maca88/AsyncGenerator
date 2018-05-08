using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	public class AssertThatNoToken
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Test()
		{
			Read();
			Assert.That(() => Read(), Throws.Nothing);
			Assert.That(() => Write(), Throws.Nothing);
			Write();
		}

		public void Write()
		{
			SimpleFile.Read();
		}
	}
}
