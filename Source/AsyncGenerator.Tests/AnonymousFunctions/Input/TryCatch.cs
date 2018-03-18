using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class TryCatch
	{
		public void Test()
		{
			Runner.RunGeneric(() =>
			{
				SimpleFile.Clear();
				return SimpleFile.Write("");
			});
		}
	}
}
