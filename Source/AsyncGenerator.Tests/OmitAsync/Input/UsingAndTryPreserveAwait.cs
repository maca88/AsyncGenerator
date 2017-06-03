using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.OmitAsync.Input
{
	public class UsingAndTryPreserveAwait
	{
		public bool AwaitShallNotBeOmittedInUsing()
		{
			using (new MemoryStream())
			{
				return SimpleFile.Write("");
			}
		}

		public bool AwaitShallNotBeOmittedInTry()
		{
			try
			{
				return SimpleFile.Write("");
			}
			catch (Exception e)
			{
				throw new AggregateException(e);
			}
		}
	}
}
