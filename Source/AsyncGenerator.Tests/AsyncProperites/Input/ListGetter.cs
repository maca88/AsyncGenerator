using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class ListGetter
	{
		public void Test()
		{
			if (List.Count > 0 || List.Contains("") || new ListGetter().List.Count > 0)
			{
				return;
			}
		}

		public List<string> List
		{
			get
			{
				SimpleFile.Read();
				return new List<string>();
			}
		}
	}
}
