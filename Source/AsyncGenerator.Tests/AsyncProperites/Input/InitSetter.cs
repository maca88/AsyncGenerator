using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class InitSetter
	{
		public string InitWriteSuccess
		{
			init { Write(value); }
		}

		public string WriteSuccess
		{
			set { Write(value); }
		}

		private bool Write(string value)
		{
			return SimpleFile.Write(value);
		}

		protected void SetSuccess()
		{
			WriteSuccess = "";
		}
	}
}
