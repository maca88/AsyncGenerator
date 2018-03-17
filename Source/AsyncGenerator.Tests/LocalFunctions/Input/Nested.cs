using AsyncGenerator.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class Nested
	{
		public void Test()
		{
			void Read()
			{
				void ReadNested()
				{
					SimpleFile.Read();
				}
				ReadNested();
			}
			Read();
		}
	}
}
