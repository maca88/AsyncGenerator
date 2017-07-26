using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class GetterWithAsyncPart
	{
		public bool WriteSuccess
		{
			get { return Write(); }
		}

		public Task<bool> GetWriteSuccessAsync()
		{
			return Task.FromResult(true);
		}

		private bool Write()
		{
			return SimpleFile.Write("");
		}

		private void CheckSuccess()
		{
			if (WriteSuccess)
			{
				return;
			}
		}
	}
}
