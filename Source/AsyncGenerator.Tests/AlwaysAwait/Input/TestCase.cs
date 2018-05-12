using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AlwaysAwait.Input
{
	public class TestCase
	{
		public void SimpleVoid()
		{
			SimpleFile.Read();
		}

		public void MultipleVoid(bool value)
		{
			SimpleFile.Clear();
			if (value)
			{
				SimpleFile.Read();
			}
			else
			{
				SimpleFile.Read();
			}
		}

		public bool SimpleRetrun()
		{
			return SimpleFile.Write("");
		}

		public bool MultipleReturns(bool value)
		{
			SimpleFile.Clear();
			if (value)
			{
				return SimpleFile.Write("A");
			}
			else
			{
				return SimpleFile.Write("B");
			}
		}

		public void NestedVoid()
		{
			SimpleVoid();
		}

		public bool Nested()
		{
			return SimpleRetrun();
		}

	}
}
