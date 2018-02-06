using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ExceptionHandling.Input
{
	public class TestCase
	{
		public bool IsValid => Validate();

		public void Read()
		{
			if (IsValid)
			{
				SimpleFile.Read();
			}
		}

		public void Read2()
		{
			if (DateTime.Today.Hour > 9)
			{
				SimpleFile.Read();
			}
		}

		private bool Validate()
		{
			return true;
		}
	}
}
