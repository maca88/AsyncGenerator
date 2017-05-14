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

		//TODO
		//public void Read3()
		//{
		//	Assert.DoesNotThrow(Read);
		//}

		//TODO
		//public void Read4()
		//{
		//	Assert.DoesNotThrow(SimpleFile.Read);
		//}

	}
}
