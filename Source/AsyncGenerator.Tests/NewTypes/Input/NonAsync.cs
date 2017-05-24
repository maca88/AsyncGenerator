using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class NonAsync : IDisposable
	{
		public void Read()
		{
			
		}


		public void Dispose()
		{
			SimpleFile.Read();
		}
	}
}
