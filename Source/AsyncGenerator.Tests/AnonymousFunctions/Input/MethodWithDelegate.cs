using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class MethodWithDelegate
	{
		public void Test()
		{
			Assert.DoesNotThrow(() => Read(s => Console.WriteLine(s)));
		}

		public void Read(Action<string> action)
		{
			SimpleFile.Read();
		}
	}
}
