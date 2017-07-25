using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class ArgumentGetter
	{
		public ArgumentGetter(bool value)
		{
			
		}

		public bool Success
		{
			get
			{
				SimpleFile.Read();
				return true;
			}
		}

		public void Test()
		{
			new ArgumentGetter(Success);
		}
	}
}
