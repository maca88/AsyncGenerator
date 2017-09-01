using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Events.Input
{
	public class AddRemoveEvent
	{
		public delegate void Delegate();

		public event Delegate Event = () => {};

		public void Test()
		{
			Event += Read;
			Event -= Read;
			Event();
		}

		public void Read()
		{
			SimpleFile.Read();
		}
	}
}
