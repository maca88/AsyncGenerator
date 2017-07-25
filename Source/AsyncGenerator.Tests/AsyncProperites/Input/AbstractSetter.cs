using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class AbstractSetter : BaseAbstractSetter
	{
		public override bool IsSuccess { get { return true; } }

		public override string WriteSuccess
		{
			set { Write(value); }
		}

		private bool Write(string value)
		{
			return SimpleFile.Write(value);
		}

		private void SetSuccess()
		{
			if (IsSuccess)
			{
				WriteSuccess = "";
			}
		}
	}

	public abstract class BaseAbstractSetter
	{
		public abstract string WriteSuccess { set; }

		public abstract bool IsSuccess { get; }
	}
}
