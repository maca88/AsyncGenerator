using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.OutsideMethodFunction.Input
{
	public class TestCase
	{
		private readonly Action _read = () => SimpleFile.Read();
		private readonly Func<string, bool> _write = content => SimpleFile.Write(content);
		private readonly Action _readDel = delegate { SimpleFile.Read(); };

		public void Read()
		{
			SimpleFile.Read();
		}

		public bool Write(string content)
		{
			return SimpleFile.Write(content);
		}
	}
}
