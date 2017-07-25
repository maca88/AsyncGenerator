using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class ArrowSetter
	{
		public string WriteSuccess
		{
			set => Write(value);
		}

		private bool Write(string value)
		{
			return SimpleFile.Write(value);
		}

		private void SetSuccess()
		{
			WriteSuccess = "";
		}
	}
}
