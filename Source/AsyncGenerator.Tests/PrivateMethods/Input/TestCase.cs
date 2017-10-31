using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PrivateMethods.Input
{
	public class TestCase
	{
		public event Action Event = () => { };

		public void Read()
		{
			SimpleFile.Read();
			Event += ReadInternal;
		}

		private void ReadInternal()
		{
			SimpleFile.Read();
		}

		protected void TriggetEvent()
		{
			Event?.Invoke();
		}
	}
}
