using AsyncGenerator.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class TryCatch
	{
		public void Test()
		{
			void Read()
			{
				SimpleFile.Clear();
				SimpleFile.Read();
			}
			Read();
		}
	}
}
