using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class SimpleSkipAsync
	{
		public bool SimpleReturn()
		{
			return SimpleFile.Write("");
		}

		public bool DoubleCallReturn()
		{
			return SimpleFile.Write(ReadFile());
		}

		public string SyncReturn()
		{
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
