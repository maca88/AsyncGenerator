using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class MethodWithDelegate
	{
		public void Test()
		{
			Read(() => SimpleFile.Read());
		}

		public void Read(Action action)
		{
			action();
			SimpleFile.Read();
		}
	}
}
