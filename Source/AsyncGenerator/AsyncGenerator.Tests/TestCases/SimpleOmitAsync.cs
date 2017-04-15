using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class SimpleOmitAsync
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

		public void SimpleVoid()
		{
			SimpleFile.Read();
		}

		public void DoubleCallVoid()
		{
			SyncReadFile();
			SimpleFile.Read();
		}

		public void ExpressionVoid() => SimpleFile.Read();

		public bool ExpressionReturn() => SimpleFile.Write("");

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
