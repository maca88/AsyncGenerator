using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class Getter
	{
		public bool WriteSuccess
		{
			get { return Write(); }
		}

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
