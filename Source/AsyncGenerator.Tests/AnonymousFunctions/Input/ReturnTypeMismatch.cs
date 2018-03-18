using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class ReturnTypeMismatch
	{
		public void Test()
		{
			Runner.Run(s =>
			{
				SimpleFile.Clear();
				SimpleFile.Write("");
			});
		}
	}
}
