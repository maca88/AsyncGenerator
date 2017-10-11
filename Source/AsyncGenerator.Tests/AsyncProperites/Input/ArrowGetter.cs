using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class ArrowGetter
	{
		public bool WriteSuccess => Write();

		private bool Write()
		{
			return SimpleFile.Write("");
		}

		protected void CheckSuccess()
		{
			if (WriteSuccess)
			{
				return;
			}
		}
	}
}
