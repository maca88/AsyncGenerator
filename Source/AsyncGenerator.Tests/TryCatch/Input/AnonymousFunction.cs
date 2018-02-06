using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class AnonymousFunction
	{
		public virtual bool Success { get; set; }

		public void Test()
		{
			Runner.Run(s =>
			{
				Success = true;
			});
		}
	}
}
