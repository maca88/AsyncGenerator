using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Nameof.Input
{
	public class Variable
	{
		public void Test()
		{
			var region = nameof(Test);
			SimpleFile.Write(region);
		}
	}
}
