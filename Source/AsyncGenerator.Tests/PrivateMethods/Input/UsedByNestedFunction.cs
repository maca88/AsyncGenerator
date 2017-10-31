using System;
using System.Collections.Generic;
using System.IO;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PrivateMethods.Input
{
	public class UsedByNestedFunction
	{
		public void Write()
		{
			Runner.Run(s =>
			{
				Test();
				SimpleFile.Write("");
			});

			var arr = new List<Action>
			{
				() =>
				{
					Test2();
					SimpleFile.Write("");
				},
				delegate
				{
					File.Open(Test3(), FileMode.Append);
				}
			};
		}

		private void Test()
		{
		}

		private void Test2()
		{
		}

		private string Test3()
		{
			return null;
		}
	}
}
