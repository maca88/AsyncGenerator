using AsyncGenerator.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.LocalFunctions.Input
{
	public class TestCase
	{
		public void Simple()
		{
			void Read()
			{
				SimpleFile.Read();
			}
			Read();
		}

		public void Expression()
		{
			void Read() => SimpleFile.Read();
			Read();
		}

	}
}
