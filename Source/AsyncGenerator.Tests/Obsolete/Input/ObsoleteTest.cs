using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Obsolete.Input
{
	public class ObsoleteTest
	{
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Read2()
		{
		}

		public Task Read2Async()
		{
			return SimpleFile.ReadAsync();
		}
	}
}
