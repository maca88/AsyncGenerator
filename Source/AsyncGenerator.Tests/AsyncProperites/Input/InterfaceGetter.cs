using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class InterfaceGetter : IInterfaceGetter
	{
		public bool IsSuccess { get { return true; } }

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
			if (WriteSuccess && IsSuccess)
			{
				return;
			}
		}
	}

	public interface IInterfaceGetter
	{
		bool WriteSuccess { get; }

		bool IsSuccess { get; }
	}
}
