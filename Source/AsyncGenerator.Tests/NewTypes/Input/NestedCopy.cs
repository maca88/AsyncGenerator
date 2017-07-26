using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public abstract class AbstractNestedCopy
	{
		protected abstract void Setup();
	}

	public class NestedCopy : AbstractNestedCopy
	{
		protected override void Setup()
		{
			Read();
		}

		public void Test()
		{
			Read();
		}

		private void Read()
		{
			InternalInternalInternalRead();
		}

		private void InternalInternalInternalRead()
		{
			InternalInternalRead();
		}

		private void InternalInternalRead()
		{
			InternalRead();
		}

		private void InternalRead()
		{
			SimpleFile.Read();
		}
	}
}
