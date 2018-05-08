using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class Variable
	{
		public void Test()
		{
			SimpleFile.Read();
			Action action = () =>
			{
				SimpleFile.Read();
			};
		}
	}
}
