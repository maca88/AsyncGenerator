using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class Precondition
	{
		public virtual bool Success { get; set; }

		public void Test()
		{
			if (Success)
			{
				throw new NotSupportedException();
			}
		}
	}
}
