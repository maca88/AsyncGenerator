using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CSharpFeatures.Input
{
	public class InModifier
	{
		public void Test(in int value, in KeyValuePair<int, string> pair = default)
		{
			SimpleFile.Read();
		}

		public bool Test2(in int value, in KeyValuePair<int, string> pair = default)
		{
			SimpleFile.Read();
			return SimpleFile.Write(value.ToString());
		}

		public void Test3(int val)
		{
			Test3Method();

			bool Test3Method(in int value = 0)
			{
				SimpleFile.Read();
				return SimpleFile.Write(value.ToString());
			}
		}
	}
}
