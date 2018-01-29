using System;
using System.IO;
using System.Text;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class ObjectCreation
	{
		public MemoryStream Test(byte[] buffer)
		{
			var test = new MemoryStream(buffer);
			return test;
		}

		public StringBuilder Test2()
		{
			var test = new StringBuilder();
			return test;
		}

		public StringBuilder Test3()
		{
			var test = new StringBuilder{Length = 1};
			return test;
		}
	}
}
