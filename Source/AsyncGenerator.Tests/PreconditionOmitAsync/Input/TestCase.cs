using System;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PreconditionOmitAsync.Input
{
	public class TestCase
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

		#region Split

		public static string PreconditionToSplit(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			for (var i = 0; i < 5; i++)
			{
				if (i == 1)
				{
					continue;
				}
				break;
			}
			SimpleFile.Read();
			return "";
		}

		#endregion Split

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
