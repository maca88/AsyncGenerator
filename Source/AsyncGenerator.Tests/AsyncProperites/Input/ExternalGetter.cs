using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class ExternalGetter
	{
		public bool Test()
		{
			if (AsyncProperties.Success)
			{
				return false;
			}
			return !AsyncProperties.Success;
		}

		public string Test2()
		{
			if (SuccessProp)
			{
				return null;
			}
			return "";
		}

		public bool SuccessProp => AsyncProperties.Success;
	}
}
