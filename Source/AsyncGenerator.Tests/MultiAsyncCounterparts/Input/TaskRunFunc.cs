using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MultiAsyncCounterparts.Input
{
	public class TaskRunFunc
	{
		//public void Test()
		//{
		//	Func<bool> func = Func;
		//	Task.Run(func);
		//}

		public void Test2()
		{
			Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			});
		}

		//public bool Func()
		//{
		//	return SimpleFile.Write("");
		//}
	}
}
