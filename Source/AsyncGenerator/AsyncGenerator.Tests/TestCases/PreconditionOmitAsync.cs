using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class PreconditionOmitAsync
	{
		public string PreconditionReturn(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			return ReadFile();
		}

		public void PreconditionVoid(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			SimpleFile.Read();
		}

		public string PreconditionToSplit(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			SimpleFile.Read();
			return "";
		}

		public string SyncPrecondition(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			return SyncReadFile();
		}

		public string ReadFile()
		{
			SimpleFile.Read();
			return "";
		}

		public string SyncReadFile()
		{
			return "";
		}
	}
}
