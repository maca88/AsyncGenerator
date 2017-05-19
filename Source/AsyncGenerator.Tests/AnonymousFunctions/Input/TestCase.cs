using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class RunFunction
	{
		public RunFunction(Action action)
		{
			action();
		}
	}

	public delegate bool WriteDelegate(string content);

	public class TestCase
	{
		public void Read()
		{
			Assert.DoesNotThrow(() =>
			{
				Console.WriteLine();
				SimpleFile.Read();
			});
		}

		public void Read2()
		{
			Assert.DoesNotThrow(() =>
			{
				Console.WriteLine();
			});
		}

		public void Read3()
		{
			Assert.DoesNotThrow(() =>
			{
				SimpleFile.Read();
			});
		}

		public void Read4()
		{
			var list = new List<RunFunction>();
			list.Add(new RunFunction(() =>
			{
				SimpleFile.Read();
			}));
			SimpleFile.Read();
		}

		public void Read5()
		{
			Assert.DoesNotThrow(SimpleFile.Read);
		}

		public void Read6()
		{
			ReadInternal(SimpleFile.Read);
		}

		public void Read7()
		{
			Assert.DoesNotThrow(SimpleFile.StreamRead);
		}

		private void ReadInternal(TestDelegate del)
		{
			del();
		}

		public void Read8()
		{
			Assert.DoesNotThrow(Read);
		}

	}
}
