using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public interface IMultilineParameters
	{
		void Test(int param1,
			string param2,
			string param3);
	}

	public class MultilineParameters : IMultilineParameters
	{
		public void Test(int param1,
			string param2,
			string param3)
		{
			SimpleFile.Read();
		}
	}
}
