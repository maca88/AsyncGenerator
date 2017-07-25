using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class TestAbstractInterfaceSetter
	{
		void Test()
		{
			SimpleFile.Read();
		}
	}

	public class ConcreteAbstractInterfaceSetter : AbstractInterfaceSetter
	{
		public override bool Success
		{
			set
			{
				SimpleFile.Read();
			}
		}
	}

	public class Concrete2AbstractInterfaceSetter : AbstractInterfaceSetter
	{
		private bool _success;

		public override bool Success { set => _success = value; }
	}

	public abstract class AbstractInterfaceSetter : IAbstractInterfaceSetter
	{
		public abstract bool Success { set; }
	}

	public interface IAbstractInterfaceSetter
	{
		bool Success { set; }
	}
}
