using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class Await
	{
		public class Reader
		{
			public Reader Read()
			{
				SimpleFile.Read();
				return this;
			}
		}


		public void Test()
		{
			var reader = new Reader();
			reader.Read().Read();

			var str = SimpleFile
				.Write("")
				.ToString();

			Assert.IsTrue(
				SimpleFile
					.Write("").Equals(true));
		}
	}
}
