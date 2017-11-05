using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ConfigurationOverride.Input
{
	public class PartialCopy
	{
		public void Read()
		{
			Copy();
			SimpleFile.Read();
		}

		public void Copy()
		{
			
		}
	}
}
