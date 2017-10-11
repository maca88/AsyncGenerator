using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class AbstractGetter : BaseAbstractGetter
	{
		public override bool IsSuccess { get { return true; } }

		public override bool WriteSuccess
		{
			get { return Write(); }
		}

		private bool Write()
		{
			return SimpleFile.Write("");
		}

		protected void CheckSuccess()
		{
			if (WriteSuccess && IsSuccess)
			{
				return;
			}
		}
	}

	public abstract class BaseAbstractGetter
	{
		public abstract bool WriteSuccess { get; }

		public abstract bool IsSuccess { get; }
	}
}
