using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public class TestAbstractInterfaceGetter
	{
		protected void Test()
		{
			SimpleFile.Read();
		}
	}

	public class ConcreteAbstractInterfaceGetter : AbstractInterfaceGetter
	{
		public override bool Success
		{
			get
			{
				SimpleFile.Read();
				return true;
			}
		}
	}

	public class Concrete2AbstractInterfaceGetter : AbstractInterfaceGetter
	{
		public override bool Success => true;
	}

	public class Concrete3AbstractInterfaceGetter : AbstractInterfaceGetter
	{
		public override bool Success { get => false; }
	}

	public abstract class AbstractInterfaceGetter : IAbstractInterfaceGetter
	{
		public abstract bool Success { get; }
	}

	public interface IAbstractInterfaceGetter
	{
		bool Success { get; }
	}
}
