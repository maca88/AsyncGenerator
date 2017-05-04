using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Generics.Input
{
	public class TestCase
	{
		public IList<IEnumerable<T>> ComplexWrite<T>(string path)
		{
			SimpleFile.Write(path);
			return new List<IEnumerable<T>>();
		}

		public IList<T> Write<T>(string path)
		{
			SimpleFile.Write(path);
			return new List<T>();
		}

		public T Read<T>()
		{
			SimpleFile.Read();
			return default(T);
		}
	}


	public class GenericTestCase<T>
	{
		private TestCase _testCase = new TestCase();

		public IList<IEnumerable<T>> ComplexWrite(string path)
		{
			return _testCase.ComplexWrite<T>(path);
		}

		public IList<T> Write(string path)
		{
			return _testCase.Write<T>(path);
		}

		public T Read()
		{
			return _testCase.Read<T>();
		}
	}

	public class StringTestCase : GenericTestCase<string>
	{
		public IList<IEnumerable<string>> StringComplexWrite(string path)
		{
			return ComplexWrite(path);
		}

		public IList<string> StringWrite(string path)
		{
			return Write(path);
		}

		public string StringRead()
		{
			return Read();
		}
	}
}
